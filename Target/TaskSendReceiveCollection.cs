using System;
using System.Collections;
using System.Collections.Generic;

namespace TaskClientServerLibrary
{
    // Декларирование сигнатуры события
    public delegate void CommandSendReceiveCollectionClear(object sender, AlarmClearCommandEventArgs e);
    public delegate void CommandSendReceiveCollectionChange(object sender, AlarmChangeCommandEventArgs e);

    public class AlarmClearCommandEventArgs : EventArgs
    {
    }

    public class AlarmChangeCommandEventArgs : EventArgs
    {
        public int Index { get; }
        public CommandForListViewItem Value { get; }

        public AlarmChangeCommandEventArgs(int index, CommandForListViewItem value) : base()
        {
            this.Index = index;
            this.Value = value;
        }
    }

    /// <summary>
    /// Коллекция для полученных или отправленных команд 
    /// для отображения на элементе ListView контрола UserControlCommand
    /// </summary>
    /// <remarks></remarks>
    public class CommandSendReceiveCollection : IEnumerable
    {
        // События
        public event CommandSendReceiveCollectionClear Cleared;
        public event CommandSendReceiveCollectionClear Clearing;
        public event CommandSendReceiveCollectionChange Deleted;
        public event CommandSendReceiveCollectionChange Added;

        // контейнер элементов
        private readonly List<CommandForListViewItem> Commands;

        /// <summary>
        /// Constructor
        /// </summary>
        public CommandSendReceiveCollection()
        {
            // инициализация коллекции
            Commands = new List<CommandForListViewItem>();
        }

        /// <summary>
        /// Добавить команду в коллекцию
        /// </summary>
        /// <param name="сommand">Элемент для добавления</param>
        public void Add(CommandForListViewItem сommand)
        {
            Commands.Add(сommand);
            OnAdded(Commands.Count - 1, сommand);
        }

        /// <summary>
        /// Добавить диапазон команд
        /// </summary>
        /// <param name="сommands">Элементы для добавления</param>
        public void AddRange(CommandForListViewItem[] сommands)
        {
            foreach (CommandForListViewItem itemCommand in сommands)
            {
                Add(itemCommand);
            }
        }

        /// <summary>
        /// Удалить команду
        /// </summary>
        /// <param name="сommand">Элемент для удаления</param>
        public void Remove(CommandForListViewItem сommand)
        {
            int index = Commands.IndexOf(сommand);
            Commands.Remove(сommand);
            OnDeleted(index, сommand);
        }

        /// <summary>
        /// Удалить команду по индексу
        /// </summary>
        /// <param name="index">Индекс элемента для удаления</param>
        public void Remove(int index)
        {
            CommandForListViewItem command = (CommandForListViewItem)Commands[index];
            Commands.RemoveAt(index);
            OnDeleted(index, command);
        }

        /// <summary>
        /// Очистить коллекцию команд
        /// </summary>
        public void Clear()
        {
            OnTaskSendCollectionClearing();
            Commands.Clear();
            OnTaskSendCollectionClear();
        }

        /// <summary>
        /// Вернуть число элементов в коллекции
        /// </summary>
        public int Count
        {
            get
            {
                return Commands.Count;
            }
        }

        /// <summary>
        /// Вставить команду по индексу
        /// </summary>
        /// <param name="index">Индекс вставляемого элемента</param>
        /// <param name="command">Элемент для вставки</param>
        public void Insert(int index, CommandForListViewItem command)
        {
            Commands.Insert(index, command);
            OnAdded(index, command);
        }

        /// <summary>
        /// Вернуть команду по индексу
        /// </summary>
        public CommandForListViewItem this[int index]
        {
            get
            {
                return Commands[index] as CommandForListViewItem;
            }
        }

        /// <summary>
        /// Вернуть команду по тексту
        /// </summary>
        public CommandForListViewItem this[string text]
        {
            get
            {
                foreach (CommandForListViewItem itemCommand in Commands)
                {
                    if (itemCommand.IndexCommamd == text)
                    {
                        return itemCommand;
                    }
                }
                return null;
            }
        }

        /// <summary>
        /// Вернуть индекс данной команду
        /// </summary>
        /// <param name="command">Элемент, чей элемент нужен</param>
        /// <returns>Индекс элемента</returns>
        public int IndexOf(CommandForListViewItem command)
        {
            return Commands.IndexOf(command);
        }

        /// <summary>
        /// Вернуть IEnumerator коллекции
        /// </summary>
        /// <returns>IEnumerator</returns>
        public IEnumerator GetEnumerator()
        {
            return Commands.GetEnumerator();
        }

        /// <summary>
        /// Элемент был добавлен
        /// </summary>
        /// <param name="index">Индекс команды</param>
        /// <param name="command">Команда</param>
        private void OnAdded(int index, CommandForListViewItem command)
        {
            Added?.Invoke(this, new AlarmChangeCommandEventArgs(index, command));
        }

        /// <summary>
        /// Элемент был удалён
        /// </summary>
        /// <param name="index">Индекс команды</param>
        /// <param name="command">Команда</param>
        private void OnDeleted(int index, CommandForListViewItem command)
        {
            Deleted?.Invoke(this, new AlarmChangeCommandEventArgs(index, command));
        }

        /// <summary>
        /// Коллекция в процессе очищения
        /// </summary>
        private void OnTaskSendCollectionClearing()
        {
            Clearing?.Invoke(this, new AlarmClearCommandEventArgs());
        }

        /// <summary>
        /// Коллекция была очищена
        /// </summary>
        private void OnTaskSendCollectionClear()
        {
            Cleared?.Invoke(this, new AlarmClearCommandEventArgs());
        }
    }
    public class CommandForListViewItem
    {
        public string IDCommamd { get; set; }
        public string Description { get; set; }
        public string CommanderID { get; set; }
        public string IndexCommamd { get; set; }
        public System.Drawing.Color Color { get; set; } = System.Drawing.Color.Magenta;

        public CommandForListViewItem(string inIDCommamd, string inDescription, string inCommanderID, string inIndexCommamd)
        {
            IDCommamd = inIDCommamd;
            Description = inDescription;
            CommanderID = inCommanderID;
            IndexCommamd = inIndexCommamd;
        }
    }
}