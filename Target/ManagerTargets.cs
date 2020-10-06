using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Collections;
using static TaskClientServerLibrary.Clobal;

namespace TaskClientServerLibrary
{
    /// <summary>
    /// ''' Управляющий класс коллекцией компьютеров на стенде 
    /// ''' </summary>
    /// ''' <remarks></remarks>
    public class ManagerTargets : IEnumerable
    {
        // В классе конфигураторе стартовой формы определяются target содержащие каналы и производится их конфигурирование
        // Далее производится создание менеджера Target(ов), где на основании включённых target создаётся коллекция Target
        // каждый Target содержит сетевые переменные каналы

        /// <summary>
        /// число созданных target
        /// </summary>
        /// <value></value>
        /// <returns></returns>
        /// <remarks></remarks>
        public int Count
        {
            get
            {
                return mDictionaryTargets.Count;
            }
        }

        /// <summary>
        /// Оболочка коллекции target
        /// </summary>
        /// <value></value>
        /// <returns></returns>
        /// <remarks></remarks>
        public Dictionary<string, Target> Targets
        {
            get
            {
                return mDictionaryTargets;
            }
        }

        /// <summary>
        /// Оболочка коллекции target
        /// </summary>
        /// <value></value>
        /// <returns></returns>
        /// <remarks></remarks>
        public List<Target> ListTargets { get; private set; }

        /// <summary>
        /// элемент коллекции
        /// </summary>
        /// <param name="indexKey"></param>
        /// <value></value>
        /// <returns></returns>
        /// <remarks></remarks>
        public Target this[string indexKey]
        {
            get
            {
                return mDictionaryTargets[indexKey];
            }
        }

        public ReaderWriterCommand ParrentReaderWriterCommand { get; }

        private Dictionary<string, Target> mDictionaryTargets = new Dictionary<string, Target>(); // внутренняя коллекция для управления target
        private int mTargetCreated = 0; // внутренний счетчик для подсчета созданных target можно использовать в заголовке

        public ManagerTargets(ReaderWriterCommand parrentReaderWriterCommandClass)
        {
            this.ParrentReaderWriterCommand = parrentReaderWriterCommandClass;
        }

        /// <summary>
        ///     ''' перечислитель
        ///     ''' </summary>
        ///     ''' <returns></returns>
        ///     ''' <remarks></remarks>
        public IEnumerator GetEnumerator()
        {
            return mDictionaryTargets.GetEnumerator();
        }

        /// <summary>
        ///     ''' удаление по номеру или имени или объекту?
        ///     ''' </summary>
        ///     ''' <param name="indexKey"></param>
        ///     ''' <remarks></remarks>
        public void Remove(ref string indexKey) // 
        {
            // удаление по номеру или имени или объекту?
            // если целый тип то по плавающему индексу, а если строковый то по ключу
            mDictionaryTargets.Remove(indexKey);
            mTargetCreated -= 1;
        }

        public void Clear()
        {
            mDictionaryTargets.Clear();
        }

        ~ManagerTargets()
        {
            mDictionaryTargets = null;
            //base.Finalize();
        }

        /// <summary>
        /// Если клиент создаётся только одна вкладка с конкретнной парой задач,
        /// если сервер, то создаются вкладки по числу установленных клиентов.
        /// </summary>
        /// <returns></returns>
        public bool LoadTargets()
        {
            bool success = false;

            try
            {
                if (ParrentReaderWriterCommand.IsServer)
                {
                    // при создании автоматом добавляется в коллекцию
                    // там проверка на корректность
                    int numberPair = 1;

                    for (int I = 1; I <= ParrentReaderWriterCommand.CountClientOrNumberClient; I++)
                    {
                        string clientName = CLIENT + I;
                        if (this.NewTarget(new Target(clientName, this, NamePipe + numberPair, NamePipe + (numberPair + 1))))
                        {
                            mDictionaryTargets[clientName].IndexRow = mDictionaryTargets.Count - 1; // индекс в таблице которая уже создана
                            mDictionaryTargets[clientName].RunListenAsync();
                        }

                        numberPair += 2;
                    }
                }
                else
                {
                    string clientName = CLIENT + ParrentReaderWriterCommand.CountClientOrNumberClient;
                    if (this.NewTarget(new Target(clientName,
                                                    this,
                                                    NamePipe + (ParrentReaderWriterCommand.CountClientOrNumberClient * 2),
                                                    NamePipe + (ParrentReaderWriterCommand.CountClientOrNumberClient * 2 - 1))))
                    {
                        mDictionaryTargets[clientName].IndexRow = mDictionaryTargets.Count - 1; // индекс в таблице которая уже создана
                        mDictionaryTargets[clientName].RunListenAsync();
                    }
                }

                ListTargets = mDictionaryTargets.Values.ToList();
                success = true;
            }
            catch (Exception ex)
            {
                string text = ex.ToString();
                MessageBox.Show(text, nameof(LoadTargets), MessageBoxButtons.OK, MessageBoxIcon.Error);
                // RegistrationEventLog.EventLog_MSG_EXCEPTION(String.Format("<{0}> {1}", CAPTION, text))
                success = false;
            }

            return success;
        }

        #region GetTarget
        /// <summary>
        /// Поиск target по URLReceiveLocation в коллекции
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        /// <remarks></remarks>
        public Target GetTargetReaderFromURL(string url)
        {
            Target foundTarget = null;

            foreach (Target itemTarget in Targets.Values)
            {
                if (itemTarget.URLReceiveLocation == url)
                {
                    foundTarget = itemTarget;
                    break;
                }
            }

            return foundTarget;
        }

        /// <summary>
        /// Поиск target по URLSendLocation в коллекции
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        /// <remarks></remarks>
        public Target GetTargetWriterFromURL(string url)
        {
            Target foundTarget = null;

            foreach (Target itemTarget in Targets.Values)
            {
                if (itemTarget.URLSendLocation == url)
                {
                    foundTarget = itemTarget;
                    break;
                }
            }

            return foundTarget;
        }

        /// <summary>
        /// Поиск target по HostName в коллекции
        /// </summary>
        /// <param name="inHostName"></param>
        /// <returns></returns>
        public Target FindTargetInManager(string inHostName)
        {
            Target foundTarget = null;

            foreach (Target itemTarget in mDictionaryTargets.Values)
            {
                if (itemTarget.HostName == inHostName)
                {
                    foundTarget = itemTarget;
                    break;
                }
            }

            if (foundTarget == null)
            {
                const string CAPTION = nameof(FindTargetInManager);
                string text = string.Format("Для компьютера с адресом {0} не найден соответствующий компьютер в коллекции.", inHostName);
                MessageBox.Show(text, CAPTION, MessageBoxButtons.OK, MessageBoxIcon.Error);
                //RegistrationEventLog.EventLog_MSG_EXCEPTION(String.Format("<{0}> {1}", CAPTION, text))
            }

            return foundTarget;
        }
        #endregion

        /// <summary>
        /// Создание нового target
        /// </summary>
        /// <param name="inNewTarget"></param>
        /// <returns></returns>
        /// <remarks></remarks>
        private bool NewTarget(Target inNewTarget)
        {
            bool success = false;

            if (mDictionaryTargets.ContainsKey(inNewTarget.HostName))
            {
                const string CAPTION = "Добавление нового компьютера";
                string text = string.Format("Компьютер с именем {0} уже загружен!", inNewTarget.HostName);
                MessageBox.Show(text, CAPTION, MessageBoxButtons.OK, MessageBoxIcon.Information);
                //RegistrationEventLog.EventLog_MSG_APPLICATION_MESSAGE(String.Format("<{0}> {1}", CAPTION, text))
                return success;
            }

            try
            {
                mDictionaryTargets.Add(inNewTarget.HostName, inNewTarget);
                mTargetCreated += 1;
                //RegistrationEventLog.EventLog_AUDIT_SUCCESS("Загрузка нового компьютера " & inNewTarget.HostName)

                if (mDictionaryTargets.ContainsKey(inNewTarget.HostName))
                {
                    success = true;
                }
                else
                {
                    success = false;
                }
            }
            catch (Exception exp)
            {
                string CAPTION = exp.Source;
                string text = exp.Message;
                MessageBox.Show(text, CAPTION, MessageBoxButtons.OK, MessageBoxIcon.Error);
                //RegistrationEventLog.EventLog_MSG_EXCEPTION(String.Format("<{0}> {1}", CAPTION, text))
                success = false;
            }

            return success;
        }
    }
}