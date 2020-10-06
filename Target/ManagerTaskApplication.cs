using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;
using static TaskClientServerLibrary.Clobal;

//'<?xml version="1.0" encoding="UTF-8"?>
//'<Tasks>
//'  <Task Name="Поставить метку" Description="Поставить метку КТ" ProcedureName="НаПослеВыполненияЗапроса" WhatModule="FormMain" Index="123">
//'      <Parameter Key="1" Value="0" Type="String" />
//'      <Parameter Key="2" Value="10" Type="String" />
//'      <Parameter Key="3" Value="20" Type="String" />
//'  </Task>
//'<Task Name="Сообщение" Description="Послать сообщение на другой компьютер" ProcedureName="Сообщение" WhatModule="FormMain" Index="123">
//'    <Parameter Key="1" Value="0" Type="String" Description="Текст посылаемого сообщеня" />
//'</Task>
//'</Tasks>
namespace TaskClientServerLibrary
{
    /// <summary>
    /// Менеджер управления разрешенными задачами сетевого межпроцессного командного обмена
    /// </summary>
    public class ManagerTaskApplication : IEnumerable
    {
        /// <summary>
        /// Коллекция описанных задач и соответствующих реальных процедур
        /// </summary>
        private Dictionary<string, TaskApplication> mCollectionsTask;
        /// <summary>
        /// Полный путь к файлу XML
        /// </summary>
        private readonly string XmlPathFileTasksClientServer;
        /// <summary>
        /// Имя файла конфигурации задач
        /// </summary>
        private const string XmlFileTasks = "TasksClientServer.xml";
        /// <summary>
        /// Загрузка, навигация и сериализация задач
        /// </summary>
        private XDocument XDoc;

        public ManagerTaskApplication(string pathResource)
        {
            mCollectionsTask = new Dictionary<string, TaskApplication>();
            XmlPathFileTasksClientServer = Path.Combine(pathResource, XmlFileTasks);

            if (FileNotExists(XmlPathFileTasksClientServer))
                MessageBox.Show($"В каталоге нет файла <{XmlFileTasks}>!", "Запуск библиотеки TaskClientServerLibrary", MessageBoxButtons.OK, MessageBoxIcon.Error);
            else
            {
                LoadXmlSettingTasksClientServer();
                PopulateCollectionsTask();
            }
        }

        /// <summary>
        /// True - файла нет
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        private bool FileNotExists(string fileName)
        {
            return !File.Exists(fileName);
        }

        public Dictionary<string, TaskApplication> Tasks
        {
            get
            {
                return mCollectionsTask;
            }
        }

        public TaskApplication this[string indexKey]
        {
            get
            {
                return mCollectionsTask[indexKey];
            }
            set
            {
                mCollectionsTask[indexKey] = value;
            }
        }

        public int Count
        {
            get
            {
                return mCollectionsTask.Count();
            }
        }

        public IEnumerator GetEnumerator()
        {
            return mCollectionsTask.GetEnumerator();
        }

        public void Remove(string indexKey)
        {
            // удаление по номеру или имени или объекту?
            // если целый тип то по плавающему индексу, а если строковый то по ключу
            mCollectionsTask.Remove(indexKey);
        }

        public void Clear()
        {
            mCollectionsTask.Clear();
        }

        ~ManagerTaskApplication()
        {
            mCollectionsTask = null;
            //base.Finalize();
        }

        public TaskApplication Add(string name, string description, string procedureName, string whatModule)
        {
            if (!ContainsKey(name))
                return null;

            TaskApplication tempTask = new TaskApplication(name, description, procedureName, whatModule);
            mCollectionsTask.Add(name, tempTask);

            return tempTask;
        }

        /// <summary>
        /// Проверка Наличия
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private bool ContainsKey(string name)
        {
            if (mCollectionsTask.ContainsKey(name))
            {
                MessageBox.Show($"Задача {name} в коллекции уже существует!", "Ошибка добавления новой задачи", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Считать Из Файла Настройки Задач
        /// </summary>
        private void LoadXmlSettingTasksClientServer()
        {
            try
            {
                XDoc = XDocument.Load(XmlPathFileTasksClientServer);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Считывание из " + XmlPathFileTasksClientServer, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Заполнить CollectionsTask
        /// </summary>
        private void PopulateCollectionsTask()
        {
            if (XDoc.Root.HasElements)
            {
                foreach (XElement itemTaskXElement in XDoc.Root.Elements("Task"))
                {
                    // добавить задачу в коллекцию
                    TaskApplication tempTask = this.Add(
                        itemTaskXElement.Attribute(COMMAND_NAME).Value,
                        itemTaskXElement.Attribute(COMMAND_DESCRIPTION).Value,
                        itemTaskXElement.Attribute(ATTR_PROCEDURE_NAME).Value,
                        itemTaskXElement.Attribute(ATTR_WHAT_MODULE).Value);
                    if (tempTask != null)
                    {
                        foreach (XElement itemParameterXElement in itemTaskXElement.Elements("Parameter"))
                        {
                            // добавить параметр для вызываемой процедуры
                            tempTask.Add(Convert.ToInt32(itemParameterXElement.Attribute(COMMAND_KEY).Value),
                                         itemParameterXElement.Attribute(COMMAND_VALUE).Value,
                                         ConvertStringToEnumTypeParam(itemParameterXElement.Attribute(COMMAND_TYPE).Value),
                                         itemParameterXElement.Attribute(COMMAND_DESCRIPTION).Value);
                        }
                    }
                }
            }
        }

        private TypeParam ConvertStringToEnumTypeParam(string strEnumTypeParam)
        {
            switch (strEnumTypeParam)
            {
                case "String":
                    {
                        return TypeParam.String;
                    }

                case "Boolean":
                    {
                        return TypeParam.Boolean;
                    }

                case "DateTime":
                    {
                        return TypeParam.DateTime;
                    }

                case "Double":
                    {
                        return TypeParam.Double;
                    }

                case "Int32":
                    {
                        return TypeParam.Int32;
                    }

                case "Object":
                    {
                        return TypeParam.Object;
                    }
                default:
                    {
                        return TypeParam.Object;
                    }
            }
        }

        // Private Function ConverStringToTypeParam(inNameType) As TypeParam
        // Dim fi As Reflection.FieldInfo = EnumType.GetField([Enum].GetName(EnumType, value))
        // Dim dna As DescriptionAttribute = DirectCast(Attribute.GetCustomAttribute(fi, GetType(DescriptionAttribute)), DescriptionAttribute)

        // If dna IsNot Nothing Then
        // Return dna.Description
        // Else
        // Return value.ToString()
        // End If
        // End Function

        // ''' <summary>
        // ''' Получить описание (Description) для перечисления
        // ''' </summary>
        // ''' <param name="EnumType"></param>
        // ''' <param name="value"></param>
        // ''' <returns></returns>
        // ''' <remarks></remarks>
        // Private Function ConvertTo(ByVal EnumType As Type, ByVal value As Object) As String
        // Dim fi As Reflection.FieldInfo = EnumType.GetField([Enum].GetName(EnumType, value))
        // Dim dna As DescriptionAttribute = DirectCast(Attribute.GetCustomAttribute(fi, GetType(DescriptionAttribute)), DescriptionAttribute)

        // If dna IsNot Nothing Then
        // Return dna.Description
        // Else
        // Return value.ToString()
        // End If
        // End Function

        /// <summary>
        /// Класс исполнения задачи из запроса (команды), запрошенного от Client.
        /// </summary>
        /// <remarks></remarks>
        public class TaskApplication : IEnumerable, ICloneable
        {
            /// <summary>
            /// Имя задачи
            /// </summary>
            /// <returns></returns>
            public string Name { get; }
            /// <summary>
            /// Описание задачи
            /// </summary>
            /// <returns></returns>
            public string Description { get; }
            /// <summary>
            /// Вызываемая процедура
            /// </summary>
            /// <returns></returns>
            public string ProcedureName { get; }
            /// <summary>
            /// Имя модуля или формы где находится вызываемая процедура
            /// </summary>
            /// <returns></returns>
            public string WhatModule { get; }
            /// <summary>
            /// Список параметров вызываемой процедуры
            /// </summary>
            /// <returns></returns>
            public Dictionary<int, Parameter> Parameters
            {
                get
                {
                    return mParameters;
                }
            }

            public Parameter this[int IndexKey]
            {
                get
                {
                    return mParameters[IndexKey];
                }
                set
                {
                    mParameters[IndexKey] = value;
                }
            }

            public int Count
            {
                get
                {
                    return mParameters.Count();
                }
            }

            public IEnumerator GetEnumerator()
            {
                return mParameters.GetEnumerator();
            }

            public void Remove(int indexKey)
            {
                // удаление по номеру или имени или объекту?
                // если целый тип то по плавающему индексу, а если строковый то по ключу
                mParameters.Remove(indexKey);
            }

            public void Clear()
            {
                mParameters.Clear();
            }

            ~TaskApplication()
            {
                mParameters = null;
                //base.Finalize();
            }

            public void Add(int number, string value, TypeParam type, string description)
            {
                if (IsContainsKey(number))
                    mParameters.Add(number, new Parameter(number, value, type, description));
            }

            private Dictionary<int, Parameter> mParameters;

            /// <summary>
            /// Создание экземпляра класса исполнения задачи из запроса (команды), запрошенного от Client.
            /// </summary>
            /// <param name="name">Имя задачи</param>
            /// <param name="description">Описание задачи</param>
            /// <param name="procedureName">Вызываемая процедура</param>
            /// <param name="whatModule">Имя модуля или формы где находится вызываемая процедура</param>
            public TaskApplication(string name, string description, string procedureName, string whatModule)
            {
                mParameters = new Dictionary<int, Parameter>();
                this.Name = name;
                this.Description = description;
                this.ProcedureName = procedureName;
                this.WhatModule = whatModule;
            }

            /// <summary>
            /// Проверка Наличия
            /// </summary>
            /// <param name="number"></param>
            /// <returns></returns>
            private bool IsContainsKey(int number)
            {
                if (mParameters.ContainsKey(number))
                {
                    MessageBox.Show($"Parameter с индексом {number} уже существует!", "Ошибка добавления параметра", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return false;
                }

                if (number < 1)
                {
                    MessageBox.Show("Номер параметра должен быть в больше 1!", "Ошибка добавления параметра", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return false;
                }

                return true;
            }

            public override string ToString()
            {
                try
                {
                    return $"Имя:{this.Name} Описание:{this.Description} Процедура:{this.ProcedureName} Класс:{this.WhatModule}";
                }
                catch
                {
                    throw new Exception("Не возможно выдать задачу в строковом формате.");
                }
            }

            /// <summary>
            /// Глубокое клонирование
            /// </summary>
            /// <returns></returns>
            public virtual object Clone()
            {
                TaskApplication cloneTask = new TaskApplication(Name, Description, ProcedureName, WhatModule);

                foreach (Parameter itemParameter in this.Parameters.Values)
                    cloneTask.Add(itemParameter.Number, itemParameter.Value, itemParameter.Type, itemParameter.Description);

                return cloneTask;
            }

            /// <summary>
            /// Параметра для исполняемой процедуры в типе её содержащей
            /// </summary>
            public class Parameter
            {
                /// <summary>
                ///  Порядковый номер параметра
                ///  </summary>
                ///  <returns></returns>
                public int Number { get; }
                /// <summary>
                ///  Значение параметра
                ///  </summary>
                ///  <returns></returns>
                public string Value { get; set; }
                /// <summary>
                ///  Системный тип параметра
                ///  </summary>
                ///  <returns></returns>
                public TypeParam Type { get; }
                /// <summary>
                /// Описание назначения параметра
                /// </summary>
                /// <returns></returns>
                public string Description { get; }

                public Parameter(int number, string value, TypeParam type, string description)
                {
                    this.Number = number;
                    this.Value = value;
                    this.Type = type;
                    this.Description = description;
                }
            }
        }
    }
}