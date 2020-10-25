using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using InOutBox.Contracts;
using System.Data.SQLite;

namespace InOutBox.Implementation
{
    public class OutBox: IOutBox
    {
        #region members
        protected string StorageFolder;

        protected string DbFile;

        protected string ConStr;

        protected string Queue;

        protected int Priority;

        protected SQLiteConnection SqlConn;
        #endregion

        #region Initialization
        public OutBox(string queue, int priority)
        {
            this.Queue = queue;
            this.Priority = priority;
        }

        public void Init(string storageFolder, string conStr, string dbFile)
        {
            this.StorageFolder = Path.Combine(storageFolder, "attachments");
            this.ConStr = conStr;
            this.DbFile = dbFile;
            if (Directory.Exists(this.StorageFolder) == false)
                Directory.CreateDirectory(this.StorageFolder);
            Open();
        }
        #endregion

        #region Collection of items
        public IEnumerable<IOutItem> All()
        {
            var cmd = SqlConn.CreateCommand();
            cmd.CommandText =
                @"
                   select id, priority from items;
               ";
            var rd = cmd.ExecuteReader();
            while (rd.Read())
            {
                yield return FromReader(rd);
            }
        }

        public IEnumerable<IOutItem> Unsent()
        {
            var cmd = SqlConn.CreateCommand();
            cmd.CommandText =
                @"
                   select id, priority from items where sent is null;
               ";
            var rd = cmd.ExecuteReader();
            while (rd.Read())
            {
                yield return FromReader(rd);
            }
        }

        public IEnumerable<IOutItem> Sent()
        {
            var cmd = SqlConn.CreateCommand();
            cmd.CommandText =
                @"
                   select id, priority from items where not sent is null;
               ";
            var rd = cmd.ExecuteReader();
            while (rd.Read())
            {
                yield return FromReader(rd);
            }
        }

        private OutItem FromReader(IDataReader rd)
        {
            return new OutItem()
            {
                Id = (int) rd.GetInt64(0),
                Priority = (int) rd.GetInt64(1),
            };
        }
        #endregion

        #region Status of Item
        public void Send(IOutItem item)
        {
            SetFlag(item, "sent");
        }

        public void Deliver(IOutItem item)
        {
            SetFlag(item, "delivered");
        }

        public void Fault(IOutItem item)
        {
            SetFlag(item, "faulted");
        }

        private void SetFlag(IOutItem item, string flag)
        {
            var cmd = SqlConn.CreateCommand();
            cmd.CommandText =
                $@"
                   Update items set {flag} = julianday('now') where id =  @id
               ";
            cmd.Parameters.Add("id", DbType.UInt32).Value = item.Id;
            cmd.ExecuteNonQuery();
        }

        private string GetScalar(string sql)
        {
            var cmd = SqlConn.CreateCommand();
            cmd.CommandText = sql;
            return cmd.ExecuteScalar().ToString();
        }
        #endregion

        #region Publish new item
        public event ItemDelegete OnAddItem;

        public IOutItem Add(string extra)
        {
            var cmd = SqlConn.CreateCommand();
            cmd.CommandText =
                @"
                   Insert into items (queue,extra,created,priority) values  (@queue,@extra,julianday('now'),@priority);
                   select last_insert_rowid();
               ";
            cmd.Parameters.Add("queue", DbType.String).Value = Queue;
            cmd.Parameters.Add("extra", DbType.String).Value = extra;
            cmd.Parameters.Add("priority", DbType.Int32).Value = Priority;
            var id = (int)(Int64)cmd.ExecuteScalar();
            return new OutItem() {Id = id, Priority = Priority};
        }

        public FileStream AddWrite(IOutItem item)
        {
            var filename = ItemFile(item);
            if (File.Exists(filename))
                File.Delete(filename);
            return new FileStream(
                filename,
                FileMode.CreateNew
            );
        }

        public void AddCommit(IOutItem item)
        {
            this.SetFlag(item, "commited");
            this.OnAddItem?.Invoke(this.Queue, item);
        }

        public void AddRollback(IOutItem item)
        {
            var filename = ItemFile(item);
            if (File.Exists(filename))
                File.Delete(filename);

            var cmd = SqlConn.CreateCommand();
            cmd.CommandText =
                @"
                   delete from items where id =  @id
               ";
            cmd.Parameters.Add("id", DbType.UInt32).Value = item.Id;
            cmd.ExecuteNonQuery();
        }
        #endregion publish new Item

        #region Database maintaince
        public const string DbVersion = "1.3";

        public void Clean(bool createnew = true)
        {
            SqlConn?.Close();
            DirectoryInfo di = new DirectoryInfo(StorageFolder);
            foreach (FileInfo file in di.GetFiles())
            {
                file.Delete();
            }
            if (File.Exists(DbFile))
                File.Delete(DbFile);
            if (createnew)
                Burn();
        }

        public void Burn()
        {
            Clean(false);
            SqlConn = new SQLiteConnection(ConStr);
            SqlConn.Open();
            var cmd = SqlConn.CreateCommand();
            cmd.CommandText =
                $@"
                    CREATE TABLE dblease (
                       version TEXT NOT NULL,
                       date real
                    );

                    Insert into dblease (version, date) values ('{DbVersion}',julianday('now'));                    

                    CREATE TABLE items (
                       id INTEGER PRIMARY KEY AUTOINCREMENT,
                       queue TEXT NOT NULL,
                       priority INTEGER,
                       extra TEXT NOT NULL,
                       created real,
                       commited real,
                       sent real,
                       delivered real,
                       faulted real,
                       retried INTEGER
                    );
               ";
            cmd.ExecuteNonQuery();
        }

        public void Open()
        {
            if (File.Exists(DbFile) == false)
            {
                Burn();
            }
            else
            {
                SqlConn = new SQLiteConnection(ConStr);
                SqlConn.Open();
            }
            var ver = GetScalar("Select version from dblease limit 1");
            if (ver != DbVersion)
            {
                Burn();
            }
        }
        #endregion

        #region Read Item
        public Stream Read(IOutItem item)
        {
            var filename = ItemFile(item);
            return new FileStream(
                filename,
                FileMode.Open
            );
        }

        public string ItemFile(IOutItem item) =>
            Path.Combine(StorageFolder, item.Id.ToString() + ".txt");
        #endregion
    }
}
