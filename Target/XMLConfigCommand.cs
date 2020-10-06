using System;
using System.Data;
using System.IO;
using System.Windows.Forms;
using System.Xml;
using static TaskClientServerLibrary.Clobal;
using static TaskClientServerLibrary.ManagerTaskApplication;

namespace TaskClientServerLibrary
{
    /// <summary>
    /// Работа по расшифровке команды полученной от Target
    /// </summary>
    /// <remarks></remarks>
    public class XMLConfigCommand
    {
        #region  Declarations 
        private DataSet mDataSet = new DataSet();
        private XmlDataDocument xmlDataDoc;
        private readonly WhoIsUpdate whoIsUpdated;
        private const string DATA_SET_COMMAND = "DataSetCommand";
        private const string KeyIsNoExist = "Запрошенный ключ не найден.";
        private const string RootIsNotExist = "Корневой узел для запрошенного ключа отсутствует.";
        #endregion

        #region  Public properties, procedures and enums
        public DataGridView DataGridCommand { get; set; } = new DataGridView();
        public ManagerTaskApplication ManagerTasks { get; set; }

        public XMLConfigCommand(WhoIsUpdate optWhoIsUpdated = WhoIsUpdate.DataView)
        {
            whoIsUpdated = optWhoIsUpdated;
            InitializeDataset();
            DataGridCommand.DataSource = GetDataTable();
        }

        public override string ToString()
        {
            string commandXML = string.Empty;

            try
            {
                commandXML = mDataSet.GetXml();
            }
            catch (Exception ex)
            {
                string CAPTION = $"Процедура <{nameof(ToString)}>  невозможно выдать таблицу в XML формате.";
                string text = ex.Message;
                MessageBox.Show(text, CAPTION, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                //RegistrationEventLog.EventLog_MSG_EXCEPTION($"<{CAPTION}> {text}")
                //Throw New Exception("Невозможно выдать таблицу в XML формате.")
            }

            return commandXML;
        }

        /// <summary>
        /// Загрузить XmlTextReader через строку в формате XML.
        /// Далее создаётся DataSet -> заполняется через XmlTextReader -> заполняется DataGridView
        /// </summary>
        /// <param name="xmlData"></param>
        public void LoadXMLfromString(string xmlData)
        {
            //<DataSetCommand>
            //  <Settings>
            //    <Key>NAME</Key>
            //    <Value>Сообщение</Value>
            //    <Type>String</Type>
            //  </Settings>
            //  <Settings>
            //    <Key>DESCRIPTION</Key>
            //    <Value>Послать сообщение на другой компьютер</Value>
            //    <Type>String</Type>
            //  </Settings>
            //  <Settings>
            //    <Key>1 ПАРАМЕТР</Key>
            //    <Value>Привет</Value>
            //    <Type>String</Type>
            //  </Settings>
            //  <Settings>
            //    <Key>INDEX</Key>
            //    <Value>811557888</Value>
            //    <Type>String</Type>
            //  </Settings>
            //</DataSetCommand>

            xmlDataDoc = null;
            mDataSet = new DataSet();
            InitializeDataset();
            // то же работает
            //Dim xmlReaderMemory As XmlReader = XmlReader.Create(New StringReader(xmlData))
            //mDataSet.ReadXml(xmlReaderMemory, XmlReadMode.InferSchema)
            // то же работает
            //Dim reader As System.IO.StringReader = New System.IO.StringReader(xmlData)

            using (XmlTextReader reader = new XmlTextReader(new StringReader(xmlData)))
            {
                try
                {
                    mDataSet.ReadXml(reader); //, XmlReadMode.InferSchema)
                    DataGridCommand.DataSource = GetDataTable();
                }
                catch (Exception ex)
                {
                    string CAPTION = $"Невозможно загрузить данные из строки  в <{nameof(LoadXMLfromString)}>.";
                    string text = ex.Message;
                    MessageBox.Show(text, CAPTION, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    //RegistrationEventLog.EventLog_MSG_EXCEPTION($"<{CAPTION}> {text}")
                }
            }
        }

        /// <summary>
        /// Добавить запись в таблицу или документ
        /// </summary>
        /// <param name="key"></param>
        /// <param name="inValue"></param>
        /// <param name="typeValue"></param>
        public void AddRow(string key, string inValue, TypeParam typeValue = TypeParam.String)
        {
            string[] EditValue = { inValue, typeValue.ToString() };

            if (whoIsUpdated == WhoIsUpdate.DataView)
            {
                //RowValue(key) = EditValue;
                SetRowValue(key, EditValue);
            }
            else
            {
                //ValuePathQuery(key) = EditValue;
                SetValuePathQuery(key, EditValue);
            }
        }

        /// <summary>
        /// Значение записи в таблице или документе
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public string[] GetRowValue(string key)
        {
            try
            {
                if (whoIsUpdated == WhoIsUpdate.DataView)
                {
                    return RowValue(key);
                }
                else
                {
                    return ValuePathQuery(key);
                }
                //MessageBox.Show("Значение = " & Value(0) & "; Тип = " & Value(1), Key, MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1, Nothing)
            }
            catch (Exception ex)
            {
                string caption = $"Функция <{nameof(GetRowValue)}> вызвала ошибку";
                string text = ex.Message;
                MessageBox.Show(text, caption, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                //RegistrationEventLog.EventLog_MSG_EXCEPTION($"<{CAPTION}> {text}")
                return new[] { 0.ToString(), TypeParam.String.ToString() };
            }
        }

        /// <summary>
        /// Значение записи в таблице DataSet
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        private string[] RowValue(string key)
        {
            DataView dv = new DataView(mDataSet.Tables[TableCommand]) { Sort = COMMAND_KEY };
            int index = dv.Find(key.ToUpper().Trim()); // Находит строку в DataView по указанному значению ключа сортировки.

            if (index > -1)
            {
                //Return dv.Item(index)(conValue)
                return new[] { dv[index][COMMAND_VALUE].ToString(), dv[index][COMMAND_TYPE].ToString() };
            }
            else
            {
                throw new Exception(KeyIsNoExist);
            }
        }

        private void SetRowValue(string key, string[] Value)
        {
            // проверить существует ли запись, прежде чем что-либо делать
            DataView dv = new DataView(mDataSet.Tables[TableCommand]) { Sort = COMMAND_KEY };
            //dv.RowFilter = "Key='" & Key.ToUpper.Trim & "'"
            int index = dv.Find(key.ToUpper().Trim());

            if (index == -1)
            {
                // запись не найдена, значить добавить строку
                string[] CellValues = { key.ToUpper().Trim(), Value[0], Value[1] };
                mDataSet.Tables[TableCommand].Rows.Add(CellValues);
            }
            else
            {
                // запись найдена, обновить новыми значениями
                dv[index][COMMAND_KEY] = key.ToUpper().Trim();
                dv[index][COMMAND_VALUE] = Value[0];
                dv[index][COMMAND_TYPE] = Value[1];
            }
        }

        /// <summary>
        /// Значение записи в документе
        /// </summary>
        /// <param name="inKey"></param>
        /// <returns></returns>
        private string[] ValuePathQuery(string inKey)
        {
            if (xmlDataDoc == null)
            {
                xmlDataDoc = new XmlDataDocument(mDataSet);
            }

            XmlNode nodRecord = xmlDataDoc.SelectSingleNode($"/{DATA_SET_COMMAND}/{Clobal.TableCommand}[Key={inKey.ToUpper().Trim()}]");

            if (nodRecord != null)
            {
                return new[] { nodRecord.ChildNodes[1].InnerText, nodRecord.ChildNodes[2].InnerText };
            }
            else
            {
                throw new Exception(KeyIsNoExist);
            }
        }

        private void SetValuePathQuery(string inKey, string[] Value)
        {
            // проверить существует ли запись, прежде чем что-либо делать
            if (xmlDataDoc == null)
            {
                xmlDataDoc = new XmlDataDocument(mDataSet);
            }

            mDataSet.EnforceConstraints = false;
            XmlNode nodRecord = xmlDataDoc.SelectSingleNode($"/{DATA_SET_COMMAND}/{Clobal.TableCommand}[Key={inKey.ToUpper().Trim()}]");

            if (nodRecord == null)
            {
                // запись не найдена, значить добавить строку
                string strXpathQueryRoot = "/" + DATA_SET_COMMAND;
                XmlNode nodRoot = xmlDataDoc.SelectSingleNode(strXpathQueryRoot);

                if (nodRoot != null)
                {
                    //<appSettings>
                    //  <Key>1</Key>
                    //  <Value>1</Value>
                    //  <Type>String</Type>
                    //</appSettings>     
                    XmlNode nodChild = xmlDataDoc.CreateNode(XmlNodeType.Element, Clobal.TableCommand, string.Empty);
                    CreateKey(ref xmlDataDoc, ref nodChild, COMMAND_KEY, inKey.ToUpper().Trim());
                    CreateKey(ref xmlDataDoc, ref nodChild, COMMAND_VALUE, Value[0]);
                    CreateKey(ref xmlDataDoc, ref nodChild, COMMAND_TYPE, Value[1]);
                    nodRoot.AppendChild(nodChild);
                }
                else
                {
                    throw new Exception(RootIsNotExist);
                }
            }
            else
            {
                // запись найдена, обновить новыми значениями
                nodRecord.ChildNodes[0].InnerText = inKey.ToUpper().Trim();
                nodRecord.ChildNodes[1].InnerText = Value[0];
                nodRecord.ChildNodes[2].InnerText = Value[1];
            }
            mDataSet.EnforceConstraints = true;
        }

        private void CreateKey(ref XmlDataDocument xmlDoc, ref XmlNode xmlNodeSection, string name, string sValue)
        {
            XmlNode newNodeKey = xmlDoc.CreateNode(XmlNodeType.Element, name, string.Empty);
            newNodeKey.InnerText = sValue;
            xmlNodeSection.AppendChild(newNodeKey);
        }

        //Public Function KeyCount() As Integer
        //    Return mDataSet.Tables(TableCommand).Rows.Count
        //End Function

        /// <summary>
        /// Удалить запись
        /// </summary>
        /// <param name="Key"></param>
        public void RemoveKey(string Key)
        {
            try
            {
                if (whoIsUpdated == WhoIsUpdate.DataView)
                {
                    Remove(Key);
                }
                else
                {
                    RemovePathQuery(Key);
                }
            }
            catch (Exception ex)
            {
                string text = ex.Message;
                MessageBox.Show(text, $"<{nameof(RemoveKey)}> Error:", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                //RegistrationEventLog.EventLog_MSG_EXCEPTION($"<{CAPTION}> {text}")
            }
        }

        /// <summary>
        /// Удалить запись в таблице
        /// </summary>
        /// <param name="Key"></param>
        private void Remove(string Key)
        {
            DataView dv = new DataView(mDataSet.Tables[TableCommand]) { Sort = COMMAND_KEY };
            int index = dv.Find(Key.ToUpper().Trim());

            if (index > -1)
            {
                dv[index].Delete();
            }
            else
            {
                throw new Exception(KeyIsNoExist);
            }
        }

        /// <summary>
        /// Удалить запись в документе
        /// </summary>
        /// <param name="Key"></param>
        private void RemovePathQuery(string Key)
        {
            if (xmlDataDoc == null)
            {
                xmlDataDoc = new XmlDataDocument(mDataSet);
            }

            mDataSet.EnforceConstraints = false;

            string strXpathQuery = $"/{DATA_SET_COMMAND}/{Clobal.TableCommand}[Key={Key.ToUpper().Trim()}]";
            XmlNode nodRecord = xmlDataDoc.SelectSingleNode(strXpathQuery);

            if (nodRecord != null)
            {
                string strXpathQueryRoot = "/" + DATA_SET_COMMAND;
                XmlNode nodRoot = xmlDataDoc.SelectSingleNode(strXpathQueryRoot);

                if (nodRoot != null)
                {
                    nodRoot.RemoveChild(nodRecord);
                }
                else
                {
                    throw new Exception(RootIsNotExist);
                }
            }
            else
            {
                throw new Exception(KeyIsNoExist);
            }
            mDataSet.EnforceConstraints = true;
        }

        public void Clear()
        {
            if (whoIsUpdated == WhoIsUpdate.XmlDataDocument)
            {
                // не работает т.к. надо заново определять DataSet
                //For I As Integer = _ds.Tables(TableCommand).Rows.Count - 1 To 0 Step -1
                //    mDataSetTables(TableCommand).Rows.RemoveAt(I)
                //Next
                //mDataSetEnforceConstraints = False
                //xmlDataDoc.RemoveAll()
                //mDataSetEnforceConstraints = True

                xmlDataDoc = null;
                mDataSet = new DataSet();
                InitializeDataset();
                DataGridCommand.DataSource = GetDataTable();
            }
            else
            {
                mDataSet.Tables[TableCommand].Rows.Clear();
            }
        }

        public DataTable GetDataTable()
        {
            return mDataSet.Tables[TableCommand];
        }

        /// <summary>
        /// Создать экземпляр задачи, клонировать его из менеджера.
        /// Заполнить значения параметров полученными по сети
        /// и вернуть новый экземпляр.
        /// </summary>
        /// <returns></returns>
        public TaskApplication GetTask()
        {
            TaskApplication mTask = (TaskApplication)(ManagerTasks.Tasks[GetRowValue(COMMAND_NAME)[0]].Clone());

            foreach (DataRow itemRow in mDataSet.Tables[TableCommand].Rows)
            {
                if (itemRow[COMMAND_KEY].ToString().IndexOf(COMMAND_PARAMETER.ToUpper()) != -1)
                {
                    //int indexCommand = Convert.ToInt32(NumericHelper.Val(strCommand));
                    mTask[Convert.ToInt32(itemRow[COMMAND_KEY].ToString().Replace(" " + COMMAND_PARAMETER.ToUpper(), ""))].Value = itemRow[COMMAND_VALUE].ToString();
                }
            }

            return mTask;
        }
        #endregion

        #region  Private procedures 
        private void InitializeDataset()
        {
            mDataSet.DataSetName = DATA_SET_COMMAND;
            mDataSet.Tables.Add(TableCommand);
            mDataSet.Tables[TableCommand].Columns.Add(COMMAND_KEY, typeof(string));
            mDataSet.Tables[TableCommand].Columns.Add(COMMAND_VALUE, typeof(string));
            mDataSet.Tables[TableCommand].Columns.Add(COMMAND_TYPE, typeof(string));
        }
        #endregion
    }
}

//'пример доступа к записям в таблице посредством запроса из справки -> Performing an XPath Query on a DataSet (ADO.NET)
//'    Dim xmlDoc As XmlDataDocument = New XmlDataDocument(DataSet)
//'    Dim nodeList As XmlNodeList = xmlDoc.DocumentElement.SelectNodes( "descendant::Customers[*/OrderDetails/ProductID=43]")
//'    Dim dataRow As DataRow
//'    Dim xmlNode As XmlNode

//'For Each xmlNode In nodeList
//'  dataRow = xmlDoc.GetRowFromElement(CType(xmlNode, XmlElement))

//'  If Not dataRow Is Nothing then Console.WriteLine(xmlRow(0).ToString())
//'Next

//'Private Const XML_Suffix As String = ".cfg"
//'Private _XMLPath As String
//'Private _AppPath As String
//'Private _ExeName As String

//'Public Sub SaveXML()
//'    Try
//'        '_ds.WriteXml(_XMLPath, XmlWriteMode.WriteSchema)
//'        _ds.WriteXml(_XMLPath)
//'    Catch
//'        Throw New Exception("Невозможно записать в " & _XMLPath & ".")
//'    End Try
//'End Sub

//'Public Property XMLPath() As String
//'    Get
//'        Return _XMLPath
//'    End Get
//'    Set(ByVal Value As String)
//'        _XMLPath = Value
//'    End Set
//'End Property

//'Public Sub ResetXMLPath()
//'    _XMLPath = _AppPath & _ExeName & XML_Suffix
//'End Sub