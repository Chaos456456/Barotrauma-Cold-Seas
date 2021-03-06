﻿using Lidgren.Network;
using System;
using System.IO;

namespace Barotrauma.Networking
{
    enum FileTransferStatus
    {
        NotStarted, Sending, Receiving, Finished, Error, Canceled
    }

    enum FileTransferMessageType
    {
        Unknown, Initiate, Submarine, Cancel
    }

    class FileStreamSender : IDisposable
    {
        public static TimeSpan MaxTransferDuration = new TimeSpan(0, 2, 0);

        private FileStream inputStream;
        private int sentOffset;
        private int chunkLen;
        private byte[] tempBuffer;
        private NetConnection connection;

        float waitTimer;

        DateTime startingTime;

        private FileTransferMessageType fileType;

        public FileTransferStatus Status
        {
            get;
            private set;
        }

        public string FileName
        {
            get;
            private set;
        }

        public string FilePath
        {
            get;
            private set;
        }

        public float Progress
        {
            get { return inputStream == null ? 0.0f : (float)sentOffset / (float)inputStream.Length; }
        }

        public int Sent
        {
            get { return sentOffset; }
        }

        public long FileSize
        {
            get { return inputStream == null ? 0 : inputStream.Length; }
        }

        public static FileStreamSender Create(NetConnection conn, string filePath, FileTransferMessageType fileType)
        {
            if (!File.Exists(filePath))
            {
                DebugConsole.ThrowError("Sending a file failed. File ''"+filePath+"'' not found.");
                return null;
            }

            FileStreamSender sender = null;

            try
            {
                sender = new FileStreamSender(conn, filePath, fileType);
            }

            catch (Exception e)
            {
                DebugConsole.ThrowError("Couldn't open file ''"+filePath+"''",e);
            }
             
            return sender;
        }

        private FileStreamSender(NetConnection conn, string filePath, FileTransferMessageType fileType)
        {
            connection = conn;
            inputStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            chunkLen = connection.Peer.Configuration.MaximumTransmissionUnit - 100;
            tempBuffer = new byte[chunkLen];
            sentOffset = 0;

            FilePath = filePath;
            FileName = Path.GetFileName(filePath);

            this.fileType = fileType;

            Status = FileTransferStatus.NotStarted;

            startingTime = DateTime.Now;
        }
        
        public void Update(float deltaTime)
        {
            if (inputStream == null || 
                Status == FileTransferStatus.Canceled || 
                Status == FileTransferStatus.Error ||
                Status == FileTransferStatus.Finished) return;

            if (DateTime.Now > startingTime + MaxTransferDuration)
            {
                CancelTransfer();
                return;
            }

            waitTimer -= deltaTime;
            if (waitTimer > 0.0f) return;
            
            if (!connection.CanSendImmediately(NetDeliveryMethod.ReliableOrdered, 1)) return;
            
            // send another part of the file!
            long remaining = inputStream.Length - sentOffset;
            int sendBytes = (remaining > chunkLen ? chunkLen : (int)remaining);

            // just assume we can read the whole thing in one Read()
            inputStream.Read(tempBuffer, 0, sendBytes);

            NetOutgoingMessage message;
            if (sentOffset == 0)
            {
                // first message; send length, chunk length and file name
                message = connection.Peer.CreateMessage(sendBytes + 8 + 1);
                message.Write((byte)PacketTypes.FileStream);
                message.Write((byte)FileTransferMessageType.Initiate);
                message.Write((byte)fileType);
                message.Write((ulong)inputStream.Length);
                message.Write(Path.GetFileName(inputStream.Name));
                connection.SendMessage(message, NetDeliveryMethod.ReliableOrdered, 1);

                Status = FileTransferStatus.Sending;
            }

            message = connection.Peer.CreateMessage(sendBytes + 8 + 1);
            message.Write((byte)PacketTypes.FileStream);
            message.Write((byte)fileType);
            message.Write(tempBuffer, 0, sendBytes);

            connection.SendMessage(message, NetDeliveryMethod.ReliableOrdered, 1);
            sentOffset += sendBytes;

            waitTimer = connection.AverageRoundtripTime;

            //Program.Output("Sent " + m_sentOffset + "/" + m_inputStream.Length + " bytes to " + m_connection);

            if (remaining - sendBytes <= 0)
            {
                //Dispose();

                Status = FileTransferStatus.Finished;
            }
        }

        public void CancelTransfer()
        {
            Status = FileTransferStatus.Canceled;
        }


        public void Dispose()
        {
            Dispose(true);

            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            inputStream.Close();
            inputStream.Dispose();
            inputStream = null;
        }
    }
}
