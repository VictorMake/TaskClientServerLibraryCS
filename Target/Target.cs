using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using static TaskClientServerLibrary.Clobal;

namespace TaskClientServerLibrary
{
    public class Target
    {
        //Public Event PropertyChanged As PropertyChangedEventHandler
        /// <summary>
        /// Событие генерируемое при приёме новой команды
        /// </summary>
        public event EventHandler<DataUpdatedEventArgs<string>> DataUpdated;
        /// <summary>
        /// Событие генериремоу при удачной отправке команды или при ошибке
        /// </summary>
        public event EventHandler<WriteCompletedEventArgs> WriteCompleted;
        /// <summary>
        /// Имя слушающего канала. 
        /// </summary>
        public string NamePiepListen { get; set; }
        /// <summary>
        /// Имя пишущего канала. 
        /// </summary>
        public string NamePiepSend { get; set; }
        /// <summary>
        /// Имя Target
        /// </summary>
        /// <returns></returns>
        public string HostName { get; set; }
        /// <summary>
        /// Родительский управляющий класс коллекцией компьютеров на стенде 
        /// </summary>
        /// <returns></returns>
        public ManagerTargets ParrentManagerTargets { get; }
        /// <summary>
        /// индекс для связи со строкой таблицы
        /// </summary>
        /// <value></value>
        /// <returns></returns>
        /// <remarks></remarks>
        public int IndexRow { get; set; }
        /// <summary>
        /// URL канала получателя
        /// </summary>
        /// <value></value>
        /// <returns></returns>
        /// <remarks></remarks>
        public string URLReceiveLocation { get; set; }
        /// <summary>
        /// URL канала отправителя
        /// </summary>
        /// <value></value>
        /// <returns></returns>
        /// <remarks></remarks>
        public string URLSendLocation { get; set; }

        /// <summary>
        /// Статус соединения канала получателя
        /// </summary>
        /// <value></value>
        /// <returns></returns>
        /// <remarks></remarks>
        public string PipeServerStatus { get; set; }

        /// <summary>
        /// Ошибка отправки команды
        /// </summary>
        /// <value></value>
        /// <returns></returns>
        /// <remarks></remarks>
        public string ErrorCommand { get; set; }

        /// <summary>
        /// Временная метка команды от target
        /// </summary>
        /// <value></value>
        /// <returns></returns>
        /// <remarks></remarks>
        public string TimeStampTarget { get; set; }

        /// <summary>
        /// XML текст команды, отправляемой на target
        /// </summary>
        /// <value></value>
        /// <returns></returns>
        /// <remarks></remarks>
        public string SendText { get; set; }

        /// <summary>
        /// Содержит текст последней команды для сравнения
        /// </summary>
        /// <value></value>
        /// <returns></returns>
        /// <remarks></remarks>
        public string LastCommand { get; set; } = null;

        private string mCommandValueReader;

        /// <summary>
        /// Прочитанное значение из транспорта команды канала получателя
        /// </summary>
        /// <value></value>
        /// <returns></returns>
        /// <remarks></remarks>       
        public string CommandValueReader
        {
            get
            {
                return mCommandValueReader;
            }
            set
            {
                // в фоне запускаются потоки ассинхронного чтения значений команд и они передаются на расшифровку и исполнение в методе Set свойства CommandValue для target
                // там в случае прихода новой команды вызвать событие приёма команды для подписчиков - формы просмотра и отправки команд
                if (IsControlCommadVisible)
                {
                    UserControlCommandTarget.ReceiveTextBox.Text = UserControlCommandTarget.ConvertStringToXML(value);
                }

                mCommandValueReader = value;
            }
        }

        /// <summary>
        /// Значение для записи в транспорт команды канала отправителя
        /// </summary>
        /// <value></value>
        /// <returns></returns>
        /// <remarks></remarks>       
        public string CommandValueWriter { get; set; }

        /// <summary>
        /// Индекс target для связи с закладкой при логировании
        /// </summary>
        /// <value></value>
        /// <returns></returns>
        /// <remarks></remarks>
        public int IndexTab { get; set; }

        /// <summary>
        /// Упаковка команды отправляемой на target
        /// </summary>
        /// <remarks></remarks>
        public XMLConfigCommand ConfigSend { get; set; }

        /// <summary>
        /// Работа по расшифровке команды полученной от target
        /// </summary>
        /// <remarks></remarks>
        public XMLConfigCommand ConfigReceive { get; set; }

        /// <summary>
        /// Флаг видимости пользовательского контрола устанавливается при загрузке 
        /// и сбрасывается при выгрузке Окна просмотра обмена командами.
        /// При True в ReaderWriterCommand должен через данный класс
        /// управлять отображением состояний данного класса  
        /// </summary>
        /// <value></value>
        /// <returns></returns>
        /// <remarks></remarks>
        public bool IsControlCommadVisible { get; set; }

        /// <summary>
        /// Пользовательский контрол отображающий состояние обмена командами между всеми target
        /// </summary>
        /// <value></value>
        /// <returns></returns>
        /// <remarks></remarks>
        public UserControlCommand UserControlCommandTarget { get; set; }
        /// <summary>
        /// Очередь команд для отправки добавленных программно или вручную
        /// </summary>
        /// <returns></returns>
        public Queue<NetCommandForTask> CommandWriterQueue { get; set; }
        private const int CAPACITY = 10; // ёмкость очереди

        // Коллекции, хранящие состояние командного обмена. При визуализации формы, 
        // эти коллекции отображают своё состояние на контролах на закладке, связанной с target
        public CommandSendReceiveCollection ListCommandsSend;
        public CommandSendReceiveCollection ListCommandsReceive;

        private readonly PipeClientServer myPipeClientServer;
        private bool isRunListen = true; // бесконечный цикл прослушивания нового соединения для получения команды

        public Target(string inHostName,
                      ManagerTargets inParrentManagerTargets,
                      string inNamePiepListen,
                      string inNamePiepSend)
        {
            HostName = inHostName;
            ParrentManagerTargets = inParrentManagerTargets;
            NamePiepListen = inNamePiepListen;
            NamePiepSend = inNamePiepSend;
            // сформировать сетевые адреса для сетевых команд
            URLReceiveLocation = NamePiepListen;
            URLSendLocation = $"\\\\{inParrentManagerTargets.ParrentReaderWriterCommand.ServerName}\\pipe\\{NamePiepSend}";
            PipeServerStatus = "Listening - OK"; // TextBoxSendStatus.Text
            CommandWriterQueue = new Queue<NetCommandForTask>(CAPACITY);
            InitializeCollections();
            myPipeClientServer = new PipeClientServer(inNamePiepListen, inNamePiepSend, inParrentManagerTargets.ParrentReaderWriterCommand.ServerName);

            ConfigSend = new XMLConfigCommand(WhoIsUpdate.DataView);
            ConfigReceive = new XMLConfigCommand(WhoIsUpdate.DataView);
            SubscribeToEvents();
        }

        //INSTANT C# NOTE: Converted event handler wireups:
        private bool EventsSubscribed = false;
        private void SubscribeToEvents()
        {
            if (EventsSubscribed)
                return;
            else
                EventsSubscribed = true;

            ListCommandsReceive.Added += ListCommandsReceive_Added;
            ListCommandsSend.Added += ListCommandsSend_Added;
        }

        public void InitializeCollections()
        {
            ListCommandsSend = new CommandSendReceiveCollection();
            ListCommandsReceive = new CommandSendReceiveCollection();
        }

        /// <summary>
        /// Получить контрол для вкладки связанный с target
        /// </summary>
        /// <returns></returns>
        public UserControlCommand GetUserControlCommandTarget()
        {
            UserControlCommandTarget = new UserControlCommand(this);
            IsControlCommadVisible = true;
            return UserControlCommandTarget;
        }

        private void ListCommandsReceive_Added(object sender, AlarmChangeCommandEventArgs e)
        {
            if (IsControlCommadVisible)
            {
                UserControlCommandTarget.RefreshListCommandsReceive();
            }
        }

        private void ListCommandsSend_Added(object sender, AlarmChangeCommandEventArgs e)
        {
            if (IsControlCommadVisible)
            {
                UserControlCommandTarget.RefreshListCommandsSend();
            }
        }

        /// <summary>
        /// Асинхронная отправка команды
        /// </summary>
        /// <param name="commandValueWriter"></param>
        /// <returns></returns>
        public async Task WriteDataAsync(string commandValueWriter)
        {
            if (IsControlCommadVisible)
            {
                UserControlCommandTarget.SetButtonSendEnabled(false);
                UserControlCommandTarget.UpdateError("");
            }

            try
            {
                await myPipeClientServer.SendAsync(commandValueWriter, 1000);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("ERROR: {0}", ex.ToString());
                //Await Task.Run(Function() Console.WriteLine("ERROR: {0}", ex.ToString))
                if (IsControlCommadVisible)
                {
                    UserControlCommandTarget.UpdateError(ex.ToString());
                }
                ErrorCommand = ex.ToString();
                WriteCompleted?.Invoke(this, new WriteCompletedEventArgs(ex, false, this));
            }

            if (IsControlCommadVisible)
            {
                UserControlCommandTarget.UpdateSendTextBox(commandValueWriter);
                UserControlCommandTarget.SetButtonSendEnabled(true);
            }
        }

        /// <summary>
        /// Запустить бесконечный цикл прослушивания
        /// </summary>
        public async void RunListenAsync()
        {
            await RunListen();
        }
        /// <summary>
        /// Запустить бесконечный цикл прослушивания
        /// </summary>
        /// <returns></returns>
        private async Task RunListen()
        {
            try
            {
                while (isRunListen)
                {
                    string messageReceive = await myPipeClientServer.Listen();

                    if (messageReceive == COMMAND_STOP)
                    {
                        isRunListen = false;
                        return;
                    }

                    DataUpdated?.Invoke(this, new DataUpdatedEventArgs<string>(new NetworkVariableData<string>(messageReceive)));
                }
            }
            catch (Exception ex)
            {
                // ошибка возникает при закрытии слушающего ожидающего подключения,
                // которая перехватывается здесь
                Debug.WriteLine("ERROR: {0}", ex.ToString());
                WriteCompleted?.Invoke(this, new WriteCompletedEventArgs(ex, false, this));
            }
        }

        /// <summary>
        /// 2  этап Close
        /// </summary>
        public void Close()
        {
            if (UserControlCommandTarget != null)
            {
                UserControlCommandTarget.Close();
                UserControlCommandTarget = null;
            }

            isRunListen = false;
            myPipeClientServer.Close();
        }
    }

    public class DataUpdatedEventArgs<TValue> : EventArgs
    {
        //
        // Сводка:
        //     Initializes a new instance of the NationalInstruments.NetworkVariable.DataUpdatedEventArgs`1
        //     class with the specified data.
        //
        // Параметры:
        //   data:
        //     The NationalInstruments.NetworkVariable.NetworkVariableData`1 that contains information
        //     about the read data.
        //
        // Исключения:
        //   T:System.ArgumentNullException:
        //     data is null.
        public DataUpdatedEventArgs(NetworkVariableData<TValue> inData)
        {
            Data = inData;
        }

        //
        // Сводка:
        //     Gets a NationalInstruments.NetworkVariable.NetworkVariableData`1 that contains
        //     information about the read data.
        public NetworkVariableData<TValue> Data { get; }
    }

    public sealed class NetworkVariableData<TValue> : ISerializable
    {
        private readonly TValue commandXML;
        public NetworkVariableData(TValue value)
        {
            HasValue = true;
            HasTimeStamp = true;
            HasQuality = true;
            TimeStamp = DateTime.Now.ToLocalTime();
            Quality = "Good";
            IsQualityGood = true;
            HasServerError = false;

            commandXML = value;
        }

        //
        // Сводка:
        //     Gets a value indicating whether the value is available.
        public bool HasValue { get; }
        //
        // Сводка:
        //     Gets a value indicating whether the NationalInstruments.NetworkVariable.NetworkVariableData`1.TimeStamp
        //     is available.
        public bool HasTimeStamp { get; }
        //
        // Сводка:
        //     Gets the timestamp of the network variable data.
        //
        // Исключения:
        //   T:System.InvalidOperationException:
        //     NationalInstruments.NetworkVariable.NetworkVariableData`1.HasTimeStamp is false.
        //
        // Примечания.
        //     The System.DateTime returned represents time in Coordinated Universal Time (UTC).
        //     To convert to local time, call System.DateTime.ToLocalTime.
        public DateTime TimeStamp { get; }
        //
        // Сводка:
        //     Gets a value indicating whether NationalInstruments.NetworkVariable.NetworkVariableData`1.Quality
        //     is available.
        public bool HasQuality { get; }
        //
        // Сводка:
        //     Gets the quality value of the data.
        //
        // Исключения:
        //   T:System.InvalidOperationException:
        //     NationalInstruments.NetworkVariable.NetworkVariableData`1.HasQuality is false.
        public string Quality { get; }
        //
        // Сводка:
        //     Gets a value indicating whether the NationalInstruments.NetworkVariable.NetworkVariableData`1.Quality
        //     value is good.
        //
        // Исключения:
        //   T:System.InvalidOperationException:
        //     NationalInstruments.NetworkVariable.NetworkVariableData`1.HasQuality is false.
        //
        // Примечания.
        //     The main purpose of this property is to differentiate between NationalInstruments.NetworkVariable.NetworkVariableData`1.Quality
        //     warnings and NationalInstruments.NetworkVariable.NetworkVariableData`1.Quality
        //     errors. If NationalInstruments.NetworkVariable.NetworkVariableData`1.IsQualityGood
        //     returns true, consider the NationalInstruments.NetworkVariable.NetworkVariableData`1.Quality
        //     value a warning, instead of an error.
        public bool IsQualityGood { get; }
        //
        // Сводка:
        //     Gets a value indicating whether NationalInstruments.NetworkVariable.NetworkVariableData`1.ServerError
        //     is available.
        public bool HasServerError { get; }
        //
        // Сводка:
        //     Gets any server or device error associated with the network variable data.
        //
        // Исключения:
        //   T:System.InvalidOperationException:
        //     NationalInstruments.NetworkVariable.NetworkVariableData`1.HasServerError is false.
        //
        // Примечания.
        //     Consult your server or device documentation for descriptions of the error codes
        //     returned by this method.
        public long ServerError { get; }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            throw new NotImplementedException();
        }

        //
        // Сводка:
        //     Returns the raw value of the network variable.
        //
        // Возврат:
        //     The raw value of the network variable.
        //
        // Исключения:
        //   T:System.InvalidOperationException:
        //     NationalInstruments.NetworkVariable.NetworkVariableData`1.HasValue is false.
        //
        // Примечания.
        //     Due to a limitation in the NationalInstruments.NetworkVariable class library,
        //     string values must be converted from a .NET Unicode string to an ANSI string.
        //     This conversion is done by using the system default code page to map each Unicode
        //     character to an ANSI character. If TValue is System.Object, System.String, or
        //     an array of either type and the value contains unmappable characters, the return
        //     value of this method replaces the unmappable characters with substitution characters.
        //     For more information, see Refer to Unicodemstudio.
        public TValue GetValue()
        {
            return commandXML;
        }
    }

    //
    // Сводка:
    //     Provides data for the NationalInstruments.NetworkVariable.NetworkVariableWriter`1.WriteCompleted
    //     event.
    public class WriteCompletedEventArgs : AsyncCompletedEventArgs
    {
        //
        // Сводка:
        //     Initializes a new instance of the NationalInstruments.NetworkVariable.WriteCompletedEventArgs
        //     class with the specified error, whether the asynchronous operation is canceled,
        //     and the optional user-supplied state object.
        //
        // Параметры:
        //   error:
        //     An error, if an error occurs, during the asynchronous operation.
        //
        //   canceled:
        //     A value indicating whether the asynchronous operation is canceled.
        //
        //   userState:
        //     The optional user-supplied state object.
        public WriteCompletedEventArgs(Exception error, bool canceled, object userState)
            : base(error, canceled, userState)
        {
        }
    }
}
//'Public Async Function WriteStopAsync() As Task
//'    Try
//'        Await myPipeClientServer.SendStopAsync()
//'    Catch ex As Exception
//'        Console.WriteLine("ERROR: {0}", ex.ToString)
//'        'Await Task.Run(Function() Console.WriteLine("ERROR: {0}", ex.ToString))
//'    End Try
//'End Function