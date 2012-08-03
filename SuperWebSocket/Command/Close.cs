﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SuperSocket.SocketBase;
using SuperSocket.SocketBase.Command;
using SuperWebSocket.Protocol;

namespace SuperWebSocket.Command
{
    /// <summary>
    /// The command handling close fragment
    /// </summary>
    /// <typeparam name="TWebSocketSession">The type of the web socket session.</typeparam>
    class Close<TWebSocketSession> : FragmentCommand<TWebSocketSession>
        where TWebSocketSession : WebSocketSession<TWebSocketSession>, new()
    {
        /// <summary>
        /// Gets the name.
        /// </summary>
        public override string Name
        {
            get
            {
                return OpCode.CloseTag;
            }
        }

        /// <summary>
        /// Executes the command.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="requestInfo">The request info.</param>
        public override void ExecuteCommand(TWebSocketSession session, IWebSocketFragment requestInfo)
        {
            var frame = requestInfo as WebSocketDataFrame;

            if (!CheckControlFrame(frame))
            {
                session.Close();
                return;
            }

            //the close handshake started from server side, now received a handshake response
            if (session.InClosing)
            {
                //Close the underlying socket directly
                session.Close(CloseReason.ClientClosing);
                return;
            }

            var data = GetWebSocketData(frame);

            int closeStatusCode = session.ProtocolProcessor.CloseStatusClode.NormalClosure;

            if (data != null && data.Length > 0)
            {
                if (data.Length == 1)
                {
                    session.Close();
                    return;
                }
                else
                {
                    var code = data[0] * 256 + data[1];

                    if (!session.ProtocolProcessor.IsValidCloseCode(code))
                    {
                        session.Close();
                        return;
                    }

                    closeStatusCode = code;

                    if (data.Length > 2)
                    {
                        this.Utf8Encoding.GetString(data, 2, data.Length - 2);
                    }
                }
            }

            //Send handshake response
            session.SendCloseHandshakeResponse(closeStatusCode);
            //After both sending and receiving a Close message, the server MUST close the underlying TCP connection immediately
            session.Close(CloseReason.ClientClosing);
        }
    }
}
