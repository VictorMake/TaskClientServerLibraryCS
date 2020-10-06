namespace TaskClientServerLibrary
{
    public static class Clobal
    {
        public const string NamePipe = "Pipe";
        public const string TableCommand = "Settings";
        //--- Для COMMAND ---
        public const string INDEX = "Index";
        public const string COMMAND_DESCRIPTION = "Description";
        public const string COMMAND_NAME = "Name";
        public const string COMMAND_PARAMETER = "Параметр";
        public const string COMMAND_COMMANDER_ID = "CommanderID";
        //--- Parameter -----
        public const string COMMAND_KEY = "Key";
        public const string COMMAND_VALUE = "Value";
        public const string COMMAND_TYPE = "Type";
        //--- Для XML ---
        public const string ATTR_PROCEDURE_NAME = "ProcedureName";
        public const string ATTR_WHAT_MODULE = "WhatModule";
        //--- Для Листа ---
        public const string ID_COMMAND_LV = "ID Команды";
        public const string COMMAND_DESCRIPTION_LV = "Описание";
        public const string COMMANDER_ID_LV = "Отправитель";
        public const string INDEX_COMMAND_LV = "Индекс Команды";

        public const string COMMAND_STOP = "Stop";
        public const string COMMAND_NOTHING = "Nothing";

        public const string CLIENT = "Клиент:";
        public const string SERVER = "Сервер";

        public const int KEY_RICH_TEXT_SERVER = -1;

        public enum WhoIsUpdate
        {
            DataView,
            XmlDataDocument
        }

        public enum TypeParam
        {
            String,
            Boolean,
            DateTime,
            Double,
            Int32,
            Object
        }

        // Список вызываемых процедур должен быть описан в XML файле "TasksClientServer.xml"
        public const string СкажиТекущееВремя = "Скажи_текущее_время";
        public const string УстановиТекущееВремя = "Установи_текущее_время";
        public const string SetPolynomialChannel = "Set_Polynomial_Channel";
        public const string OkSetPolynomialChannel = "Ok_Set_Polynomial_Channel";
        public const string ПоставитьМеткуКТ = "Поставить_метку_КТ";
        public const string ОтветПоставитьМеткуКТ = "Ответ_Поставить_метку_КТ";
        public const string StopClient = "Stop_Client";
        public const string OkStopClient = "Ok_Stop_Client";
        public const string SendMessage = "Send_Message";
        public const string OKSendMessage = "Ok_Send_Message";
        //Public Const  As String = 
    }
}