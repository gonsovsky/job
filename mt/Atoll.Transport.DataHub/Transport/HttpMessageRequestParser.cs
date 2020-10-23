using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Net.Http.Headers;
using System;
using System.Threading.Tasks;
using Atoll.Transport;
using System.Buffers;
using System.Threading;
using Atoll.Transport.Contract;

namespace Atoll.Transport.DataHub
{


    public class HttpMessageRequestParser : ITransportRequestService
    {
        private readonly IPacketsStore packetsStore;

        public HttpMessageRequestParser(IPacketsStore packetsStore)
        {
            this.packetsStore = packetsStore;
        }

        public MessageHeaders GetHeaders(HttpRequest request)
        {
            // FirstOrDefault
            var dict = request.Query.ToDictionary(x => x.Key, x => x.Value.FirstOrDefault());
            return MessageHeaders.FromDictionary(dict);
        }

        public async Task<ParseBodyAndSavePacketsResult> ParseBodyAndSavePackets(MessageHeaders messageHeaders, HttpRequest request)
        {
            var cancellationToken = request.HttpContext.RequestAborted;
            cancellationToken.ThrowIfCancellationRequested();

            var format = messageHeaders.Format ?? TransportConstants.RequestFormDataFormat;
            string boundary = null;
            if (format == TransportConstants.RequestFormDataFormat)
            {
                int fileSectionBufferSize = 81920;

                // если данные formdata упорядочены в потоке запроса (бинарные данные должны идти после большинства метаданных о пакете)
                if (messageHeaders.Hints?.Contains(MessageHints.OrderedFormData) == true)
                {
                    var indexToPacketDataItem = new Dictionary<int, PacketFormDataItem>();
                    var indexToPacketBytes = new Dictionary<int, byte[]>();
                    var indexToConfigurationItem = new Dictionary<int, ConfigurationRequestDataItem>();

                    if (MediaTypeHeaderValue.TryParse(request.ContentType, out var contentType))
                    {
                        boundary = HeaderUtilities.GetBoundary(contentType, 70);
                    }

                    var multipartReader = new MultipartReader(boundary, request.Body)
                    {
                        //ValueCountLimit = _options.ValueCountLimit,
                        //KeyLengthLimit = _options.KeyLengthLimit,
                        //ValueLengthLimit = _options.ValueLengthLimit,

                        HeadersCountLimit = int.MaxValue,
                        HeadersLengthLimit = int.MaxValue,
                        BodyLengthLimit = long.MaxValue,
                    };

                    //PacketFormDataItem current = null;
                    //byte[] currentBytes = null;
                    var agentId = messageHeaders.GetAgentIdData();
                    var section = await multipartReader.ReadNextSectionAsync(cancellationToken);
                    while (section != null)
                    {
                        // Parse the content disposition here and pass it further to avoid reparsings
                        if (!ContentDispositionHeaderValue.TryParse(section.ContentDisposition, out var contentDisposition))
                        {
                            throw new InvalidDataException("Form section has invalid Content-Disposition value: " + section.ContentDisposition);
                        }

                        if (contentDisposition.IsFileDisposition())
                        {
                            var fileSection = new FileMultipartSection(section, contentDisposition);

                            var name = fileSection.Name;
                            var fileName = fileSection.FileName;
                            var packetId = fileSection.FileName;
                            var result = GetFormPathDataFromEntry(name);
                            if (result != null && result.Parts.Count == 3 && result.Parts[0] == TransportConstants.FormDataPacketsProp)
                            {
                                if (int.TryParse(result.Parts[1], out var index))
                                {
                                    var item = indexToPacketDataItem[index];
                                    var providerKey = item.ProviderKey;

                                    var packetItem = indexToPacketDataItem[index];
                                    if (packetItem.PacketId != packetId)
                                    {
                                        throw new InvalidDataException($"Incorrect format for form-data message. Section {name} has invalid FileName.");
                                    }

                                    var bytes = await ReadToEnd(fileSection.FileStream, fileSectionBufferSize, cancellationToken);
                                    indexToPacketBytes.Add(index, bytes);
                                }
                                else
                                {
                                    throw new InvalidDataException($"Incorrect format for form-data message. Section {name} does not have index suffix.");
                                }
                            }
                            else
                            {
                                throw new InvalidDataException($"Incorrect format for form-data message. Section {name} incorrect.");
                            }
                        }
                        else if (contentDisposition.IsFormDisposition())
                        {
                            var formDataSection = new FormMultipartSection(section, contentDisposition);

                            // Content-Disposition: form-data; name="key"
                            //
                            // value

                            // Do not limit the key name length here because the multipart headers length limit is already in effect.
                            var key = formDataSection.Name;
                            var value = await formDataSection.GetValueAsync();

                            var result = GetFormPathDataFromEntry(key);
                            if (result != null && result.Parts.Count == 3 && result.Parts[0] == TransportConstants.FormDataPacketsProp)
                            {
                                if (int.TryParse(result.Parts[1], out var index))
                                {
                                    if (!indexToPacketDataItem.TryGetValue(index, out var dataItem))
                                    {
                                        //// сохраняем предыдущий
                                        //if (current != null)
                                        //{
                                        //    await this.packetsStore.AddIfNotExistsPacketPartAsync(agentId, this.CreateAddPacketRequest(current, currentBytes));
                                        //}

                                        dataItem = new PacketFormDataItem();
                                        indexToPacketDataItem.Add(index, dataItem);                                        
                                    }

                                    if (!dataItem.FillProperty(result.Parts[2], value))
                                    {
                                        throw new InvalidDataException($"Incorrect format for form-data message. Section {key} incorrect.");
                                    }
                                }
                                else
                                {
                                    throw new InvalidDataException($"Incorrect format for form-data message. Section {key} does not have index suffix.");
                                }
                            }
                            else if (result != null && result.Parts.Count == 3 && result.Parts[0] == TransportConstants.FormDataConfigurationProp)
                            {
                                if (int.TryParse(result.Parts[1], out var index))
                                {
                                    if (!indexToConfigurationItem.TryGetValue(index, out var dataItem))
                                    {
                                        dataItem = new ConfigurationRequestDataItem();
                                        indexToConfigurationItem.Add(index, dataItem);
                                    }

                                    if (!dataItem.FillProperty(result.Parts[2], value))
                                    {
                                        throw new InvalidDataException($"Incorrect format for form-data message. Section {key} incorrect.");
                                    }
                                }
                                else
                                {
                                    throw new InvalidDataException($"Incorrect format for form-data message. Section {key} does not have index suffix.");
                                }
                            }
                            else
                            {
                                // ignore or throw?
                            }

                            //if (formAccumulator.ValueCount > _options.ValueCountLimit)
                            //{
                            //    throw new InvalidDataException($"Form value count limit {_options.ValueCountLimit} exceeded.");
                            //}
                        }
                        else
                        {
                            System.Diagnostics.Debug.Assert(false, "Unrecognized content-disposition for this section: " + section.ContentDisposition);
                        }

                        section = await multipartReader.ReadNextSectionAsync(cancellationToken);
                    }

                    // сохраняем все
                    var addResult = indexToPacketDataItem.Any()
                                ? await this.packetsStore.AddIfNotExistsPacketsPartsAsync(agentId, indexToPacketDataItem.Select(x =>
                                        {
                                            var bytes = indexToPacketBytes[x.Key];
                                            return this.CreateAddPacketRequest(agentId, x.Value, bytes);
                                        }).ToList())
                                : AddAddPacketsPartsResult.EmptyResult();

                    return new ParseBodyAndSavePacketsResult
                    {
                        TransferedPackets = indexToPacketDataItem.Values
                            .Select(x => {
                                var fromAddResult = addResult.Results.Single(r => r.Request.PacketId == x.PacketId);
                                return new TransferedPacketResponse
                                {
                                    PacketId = x.PacketId,
                                    ProviderKey = x.ProviderKey,
                                    //AgentIdData = agentId,
                                    Result = fromAddResult.Success 
                                                ? TransferedProcessingResult.Saved 
                                                : TransferedProcessingResult.Error,
                                    StorageToken = fromAddResult.StorageToken,
                                    Id = fromAddResult.Id,
                                };
                            }).ToList(),
                        ConfigurationsStats = indexToConfigurationItem.Values,
                    };
                }
                else
                {
                    var indexToPacketDataItem = new Dictionary<string, PacketFormDataItem>();
                    var indexToPacketBytes = new Dictionary<string, byte[]>();
                    var indexToConfigurationItem = new Dictionary<string, ConfigurationRequestDataItem>();
                    var agentId = messageHeaders.GetAgentIdData();

                    foreach (var item in request.Form)
                    {
                        var key = item.Key;
                        var value = item.Value;

                        var result = GetFormPathDataFromEntry(key);
                        if (result != null && result.Parts.Count == 3 && result.Parts[0] == TransportConstants.FormDataPacketsProp)
                        {
                            var index = result.Parts[1];
                            if (!indexToPacketDataItem.TryGetValue(index, out var dataItem))
                            {
                                dataItem = new PacketFormDataItem();
                                indexToPacketDataItem.Add(index, dataItem);
                            }

                            if (!dataItem.FillProperty(result.Parts[2], value))
                            {
                                throw new InvalidDataException($"Incorrect format for form-data message. Section {key} incorrect.");
                            }
                        }

                        if (result != null && result.Parts.Count == 3 && result.Parts[0] == TransportConstants.FormDataConfigurationProp)
                        {
                            var index = result.Parts[1];
                            if (!indexToConfigurationItem.TryGetValue(index, out var dataItem))
                            {
                                dataItem = new ConfigurationRequestDataItem();
                                indexToConfigurationItem.Add(index, dataItem);
                            }

                            if (!dataItem.FillProperty(result.Parts[2], value))
                            {
                                throw new InvalidDataException($"Incorrect format for form-data message. Section {key} incorrect.");
                            }
                        }
                    }

                    foreach (var file in request.Form.Files)
                    {
                        var pair = indexToPacketDataItem
                                        .FirstOrDefault(x => x.Value.PacketId == file.FileName);

                        if (default(KeyValuePair<string, PacketFormDataItem>).Equals(pair))
                        {
                            var item = pair.Value;
                            var providerKey = item.ProviderKey;
                            var packetId = item.PacketId;

                            using (var fileStream = file.OpenReadStream())
                            {
                                var currentBytes = await ReadToEnd(fileStream, fileSectionBufferSize, cancellationToken);
                                indexToPacketBytes.Add(pair.Key, currentBytes);
                            }
                        }
                    }

                    // сохраняем все
                    var addResult = indexToPacketDataItem.Any()
                                ? await this.packetsStore.AddIfNotExistsPacketsPartsAsync(agentId, indexToPacketDataItem.Select(x =>
                                            {
                                                var bytes = indexToPacketBytes[x.Key];
                                                return this.CreateAddPacketRequest(agentId, x.Value, bytes);
                                            }).ToList())
                                : AddAddPacketsPartsResult.EmptyResult();

                    return new ParseBodyAndSavePacketsResult
                    {
                        TransferedPackets = indexToPacketDataItem.Values
                                        .Select(x =>
                                        {
                                            var fromAddResult = addResult.Results
                                                                  .Single(r => r.Request.PacketId == x.PacketId);
                                            return new TransferedPacketResponse
                                            {
                                                PacketId = x.PacketId,
                                                ProviderKey = x.ProviderKey,
                                                //AgentIdData = agentId,
                                                Result = fromAddResult.Success
                                                                    ? TransferedProcessingResult.Saved
                                                                    : TransferedProcessingResult.Error,
                                                StorageToken = fromAddResult.StorageToken,
                                                Id = fromAddResult.Id,
                                            };
                                        }).ToList(),
                        ConfigurationsStats = indexToConfigurationItem.Values,
                    };
                }

            }
            else
            {
                throw new NotSupportedException($"format {format} is not supported");
            }
        }

        private AddPacketPartRequest CreateAddPacketRequest(AgentIdentity agentIdentifierData, PacketFormDataItem item, byte[] bytes)
        {
            return new AddPacketPartRequest
            {
                PacketId = item.PacketId,
                ProviderKey = item.ProviderKey,
                StartPosition = item.StartPosition,
                EndPosition = item.EndPosition,
                IsFinal = item.IsFinal,
                Bytes = bytes,
                PreviousPartStorageToken = item.PreviousPartStorageToken,
                PreviousPartId = item.PreviousPartId,
                //AgentId = agentIdentifierData,
            };
        }

        public static async Task<byte[]> ReadToEnd(Stream stream, int bufferSize = 4096, CancellationToken token = default(CancellationToken))
        {
            byte[] readBuffer = new byte[bufferSize];

            int totalBytesRead = 0;
            int bytesRead;

            while ((bytesRead = await stream.ReadAsync(readBuffer, totalBytesRead, readBuffer.Length - totalBytesRead, token)) > 0)
            {
                totalBytesRead += bytesRead;

                if (totalBytesRead == readBuffer.Length)
                {
                    int nextByte = stream.ReadByte();
                    if (nextByte != -1)
                    {
                        token.ThrowIfCancellationRequested();
                        byte[] temp = new byte[readBuffer.Length * 2];                        
                        Buffer.BlockCopy(readBuffer, 0, temp, 0, readBuffer.Length);
                        Buffer.SetByte(temp, totalBytesRead, (byte)nextByte);
                        readBuffer = temp;
                        totalBytesRead++;
                    }
                }
            }

            byte[] buffer = readBuffer;
            if (readBuffer.Length != totalBytesRead)
            {
                token.ThrowIfCancellationRequested();
                buffer = new byte[totalBytesRead];
                Buffer.BlockCopy(readBuffer, 0, buffer, 0, totalBytesRead);
            }

            return buffer;
        }

        private static readonly char[] Delimiters = new char[] { '[', '.' };

        class FormDataKeyParseResult
        {
            public List<string> Parts { get; set; }
        }

        //private static readonly char[] Delimiters2 = new char[] { '[', '.', ']' };
        //private static readonly string[] Delimiters2Str = Delimiters2.Select(x=> x.ToString()).ToArray();
        private FormDataKeyParseResult GetFormPathDataFromEntry(string fullEntry)
        {
            //fullEntry.Split(Delimiters2);

            //return new FormDataKeyParseResult
            //{
            //    Parts = fullEntry.Split(Delimiters2).Except(Delimiters2Str).ToList(),
            //};

            var entry = fullEntry;
            var delimiterPosition = entry.IndexOfAny(Delimiters);

            var parts = new List<string>();
            string part;
            while (delimiterPosition > -1)
            {
                switch (entry[delimiterPosition])
                {
                    case '.':
                        // Handle an entry such as "prefix.key", "prefix.key.property" and "prefix.key[index]".
                        var nextDelimiterPosition = entry.IndexOfAny(Delimiters, delimiterPosition + 1);
                        if (nextDelimiterPosition == -1)
                        {
                            // Neither '.' nor '[' found later in the name. Use rest of the string.
                            if (delimiterPosition < entry.Length - 1)
                            {
                                part = entry.Substring(delimiterPosition + 1);
                                parts.Add(part);
                            }
                            else
                            {
                                parts.Add(string.Empty);
                            }
                            
                            entry = string.Empty;
                        }
                        else
                        {
                            part = entry.Substring(0, delimiterPosition);
                            parts.Add(part);

                            part = entry.Substring(delimiterPosition + 1, nextDelimiterPosition - delimiterPosition - 1);                            
                            parts.Add(part);

                            if (nextDelimiterPosition < entry.Length - 1)
                            {
                                entry = entry.Substring(nextDelimiterPosition + 1);
                            }
                            else
                            {
                                entry = string.Empty;
                            }
                        }
                        break;
                    case '[':
                        // Handle an entry such as "prefix[key]".
                        var bracketPosition = entry.IndexOf(']', delimiterPosition);
                        if (bracketPosition == -1)
                        {
                            // Malformed
                            return null;
                        }

                        part = entry.Substring(0, delimiterPosition);
                        parts.Add(part);
                        part = entry.Substring(delimiterPosition + 1, bracketPosition - delimiterPosition - 1);
                        parts.Add(part);

                        if (bracketPosition < entry.Length - 1)
                        {
                            entry = entry.Substring(bracketPosition + 1);
                        }
                        else
                        {
                            entry = string.Empty;
                        }
                        break;

                    default:
                        // Ignore.
                        break;
                }

                delimiterPosition = entry.IndexOfAny(Delimiters);
            }

            return new FormDataKeyParseResult
            {
                Parts = parts,
            };
        }
    }

}
