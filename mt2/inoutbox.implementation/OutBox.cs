using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using InOutBox.Contracts;
using System.Data.SQLite;
using System.Threading;

namespace InOutBox.Implementation
{
    public class OutBox: IOutBox
    {
        protected string StorageFolder;

        protected string DbFile;

        protected string ConStr;

        protected string Queue;

        protected SQLiteConnection SqlConn;

        public OutBox(string queue)
        {
            this.Queue = queue;
        }

        public void Init(string storageFolder, string conStr, string dbFile)
        {
            this.StorageFolder = storageFolder;
            this.ConStr = conStr;
            this.DbFile = dbFile;

            if (File.Exists(DbFile))
                File.Delete(DbFile);
            SqlConn = new SQLiteConnection(ConStr);
            SqlConn.Open();

            var cmd = SqlConn.CreateCommand();
            cmd.CommandText =
                @"
                    CREATE TABLE items (
                       id INTEGER PRIMARY KEY AUTOINCREMENT,
                       queue TEXT NOT NULL,
                       extra TEXT NOT NULL,
                       commited real,
                       sent real,
                       delivered real,
                       faulted real,
                       retried INTEGER
                    );
               ";
            cmd.ExecuteNonQuery();
        }

        public int Add(string extra)
        {
            var cmd = SqlConn.CreateCommand();
            cmd.CommandText =
                @"
                   Insert into items (queue,extra) values  (@queue,@extra);
                   select last_insert_rowid();
               ";
            cmd.Parameters.Add("queue", DbType.String).Value = Queue;
            cmd.Parameters.Add("extra", DbType.String).Value = extra;
            var id = (int)(Int64)cmd.ExecuteScalar();
            return id;
        }

        private void SetFlag(int itemId, string flag)
        {
            var cmd = SqlConn.CreateCommand();
            cmd.CommandText =
                $@"
                   Update items set {flag} = julianday('now') where id =  @id
               ";
            cmd.Parameters.Add("id", DbType.UInt32).Value = itemId;
            cmd.ExecuteNonQuery();
        }

        public void AddCommit(int itemId)
        {
            this.SetFlag(itemId, "commited");
            this.OnNewItem?.Invoke(this.Queue, itemId);
        }

        public void AddRollback(int itemId)
        {
            var filename = ItemFile(itemId);
            if (File.Exists(filename))
                File.Delete(filename);

            var cmd = SqlConn.CreateCommand();
            cmd.CommandText =
                @"
                   delete from items where id =  @id
               ";
            cmd.Parameters.Add("id", DbType.UInt32).Value = itemId;
            cmd.ExecuteNonQuery();
        }

        public IEnumerable<int> All()
        {
            var cmd = SqlConn.CreateCommand();
            cmd.CommandText =
                @"
                   select id from items;
               ";
            var rd = cmd.ExecuteReader();
            while (rd.Read())
            {
                yield return (int)rd.GetInt64(0);
            }
        }

        public IEnumerable<int> Unsent()
        {
            var cmd = SqlConn.CreateCommand();
            cmd.CommandText =
                @"
                   select id from items where sent is null;
               ";
            var rd = cmd.ExecuteReader();
            while (rd.Read())
            {
                yield return (int)rd.GetInt64(0);
            }
        }

        public IEnumerable<int> Sent()
        {
            var cmd = SqlConn.CreateCommand();
            cmd.CommandText =
                @"
                   select id from items where not sent is null;
               ";
            var rd = cmd.ExecuteReader();
            while (rd.Read())
            {
                yield return (int)rd.GetInt64(0);
            }
        }

        public string ItemFile(int itemId) =>
            Path.Combine(StorageFolder, itemId.ToString() + ".txt");

        public FileStream AddWrite(int itemId)
        {
            var filename = ItemFile(itemId);
            if (File.Exists(filename))
                File.Delete(filename);
            return new FileStream(
                filename,
                FileMode.CreateNew
            );
        }

        public Stream Read(int itemId)
        {
            var filename = ItemFile(itemId);
            return new FileStream(
                filename,
                FileMode.Open
            );
        }

        public void Send(int itemId)
        {
            SetFlag(itemId, "sent");
        }

        public void Deliver(int itemId)
        {
            SetFlag(itemId, "delivered");
        }

        public void Fault(int itemId)
        {
            SetFlag(itemId, "faulted");
        }

        public event ItemDelegete OnNewItem;
    }
}
