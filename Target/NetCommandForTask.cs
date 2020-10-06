namespace TaskClientServerLibrary
{
    /// <summary>
    /// Proxy для для команды на исполнения.
    /// Ставится в очередь задач для отправки.
    /// </summary>
    public class NetCommandForTask
    {
        /// <summary>
        /// Имя процедуры в форме
        /// </summary>
        /// <returns></returns>
        public string ProcedureName { get; set; }
        /// <summary>
        /// Параметры для процедуры
        /// </summary>
        /// <returns></returns>
        public string[] Parameters { get; set; }
        /// <summary>
        /// Признак того, что эта команда послана как ответ от пришедшей команды,
        /// для того чтобы передать Index
        /// </summary>
        /// <returns></returns>
        public bool IsResponse { get; set; }
        /// <summary>
        /// Индекс пришедшей команды, который вставляется вместо генератора,
        /// для поиска и отметки что от посланной команды пришёл ответ
        /// </summary>
        /// <returns></returns>
        public string IndexResponse { get; set; }

        public NetCommandForTask(string inProcedureName, params string[] inParameters)
        {
            ProcedureName = inProcedureName;

            if (!(inParameters == null))
            {
                Parameters = inParameters;
            }
        }
    }
}