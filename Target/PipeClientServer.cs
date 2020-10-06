using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Threading.Tasks;
using static TaskClientServerLibrary.Clobal;

namespace TaskClientServerLibrary
{
    /// <summary>
    /// Пара каналов асинхронного чтения и записи команд, 
    /// при командном обмене между компьютерами для выполнении на них задач
    /// </summary>
    public class PipeClientServer
    {
        /// <summary>
        /// Имя слушающего канала. 
        /// </summary>
        public string NamePiepListen { get; }
        /// <summary>
        /// Имя пишущего канала. 
        /// </summary>
        public string NamePiepSend { get; }

        public string serverName;
        private NamedPipeServerStream myPipeServer;

        public PipeClientServer(string inNamePiepListen, string inNamePiepSend, string inServerName)
        {
            NamePiepListen = inNamePiepListen;
            NamePiepSend = inNamePiepSend;
            serverName = inServerName;
        }

        /// <summary>
        /// Сервер ждет NamedPipeClientStream объект в дочерний процесс для подключения к нему.
        /// </summary>
        /// <returns></returns>
        public async Task<string> Listen()
        {
            try
            {
                myPipeServer = new NamedPipeServerStream(NamePiepListen, PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
                //pipeServer.WaitForConnection()
                await Task.Factory.FromAsync(myPipeServer.BeginWaitForConnection, myPipeServer.EndWaitForConnection, null);
                using (StreamReader reader = new StreamReader(myPipeServer))
                {
                    string text = await reader.ReadToEndAsync();
                    return text;
                }
            }
            catch (IOException ex)
            {
                Debug.WriteLine("ERROR: {0}", ex.ToString());
                //Return ($"ERROR: {ex.ToString}")
                // "Все копии канала заняты."
                if (ex.Message.Contains("Все копии канала заняты"))
                {
                    return COMMAND_STOP;
                }
                else
                {
                    return COMMAND_NOTHING;
                }
                //Finally
                //    myPipeServer.Close()
                //    myPipeServer = Nothing
            }
        }

        /// <summary>
        /// Отправить сообщение по именованному каналу.
        /// </summary>
        /// <param name="SendStr"></param>
        /// <param name="TimeOut"></param>
        /// <returns></returns>
        public async Task SendAsync(string SendStr, int TimeOut = 1000)
        {
            using (NamedPipeClientStream pipeStream = new NamedPipeClientStream(serverName, NamePiepSend, PipeDirection.Out, PipeOptions.Asynchronous))
            {
                try
                {
                    pipeStream.Connect(TimeOut);
                    Console.WriteLine($"[{serverName}] Pipe создал соединение");

                    using (StreamWriter sw = new StreamWriter(pipeStream))
                    {
                        await sw.WriteAsync(SendStr);
                        await pipeStream.FlushAsync();
                    }
                }
                catch (TimeoutException e)
                {
                    Debug.WriteLine($"ERROR: {e.Message}");
                    throw;
                }
                catch (IOException e)
                {
                    Debug.WriteLine($"ERROR: {e.Message}");
                    throw;
                }
            }
        }

        /// <summary>
        /// 3 - этап Close
        /// </summary>
        public void Close()
        {
            if (myPipeServer != null && myPipeServer.IsConnected)
            {
                myPipeServer.Disconnect();
            }

            myPipeServer.Close();
            myPipeServer = null;
        }
    }
}
//'''' <summary>
//'''' Сервер ждет NamedPipeClientStream объект в дочерний процесс для подключения к нему.
//'''' </summary>
//'''' <param name = "PipeName" ></ param >
//'''' <returns></returns>
//'Public Shared Async Function Listen(ByVal PipeName As String) As Task(Of String)
//'    Using pipeServer As NamedPipeServerStream = New NamedPipeServerStream(PipeName, PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous)
//'        'pipeServer.WaitForConnection()
//'        Await Task.Factory.FromAsync(AddressOf pipeServer.BeginWaitForConnection, AddressOf pipeServer.EndWaitForConnection, Nothing)
//'        Using reader As StreamReader = New StreamReader(pipeServer)
//'            Dim text As String = Await reader.ReadToEndAsync()
//'            Return text
//'        End Using
//'    End Using
//'End Function

//'''' <summary>
//'''' Послать команду остановки для своего же слушающего сервера(NamePiepListen).
//'''' </summary>
//'''' <param name = "TimeOut" ></ param >
//'''' <returns></returns>
//'Public Async Function SendStopAsync(ByVal Optional TimeOut As Integer = 100) As Task
//'    Using pipeStream As NamedPipeClientStream = New NamedPipeClientStream(".", NamePiepListen, PipeDirection.Out, PipeOptions.Asynchronous)
//'        Try
//'            pipeStream.Connect(TimeOut)
//'            Using sw As StreamWriter = New StreamWriter(pipeStream)
//'                Await sw.WriteAsync(COMMAND_STOP)
//'                'TODO: Await pipeStream.FlushAsync()
//'            End Using

//'        Catch e As TimeoutException
//'            Console.WriteLine($"ERROR: {e.Message}")
//'            Throw
//'        Catch e As IOException
//'            Console.WriteLine($"ERROR: {e.Message}")
//'            Throw
//'        End Try
//'    End Using
//'End Function

//'Private Shared Async Function ReadМessageAsync(ByVal s As PipeStream) As Task(Of Byte())
//'    Dim ms As MemoryStream = New MemoryStream()
//'    Dim buffer As Byte() = New Byte(4095) {}

//'    Do
//'        ms.Write(buffer, 0, Await s.ReadAsync(buffer, 0, buffer.Length))
//'    Loop While Not s.IsMessageComplete

//'    Return ms.ToArray()
//'End Function