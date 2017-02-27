using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageTagWPF.Code
{
    public enum ProcessRecommendedOutput
    {
        None,
        CompareFiles,
        FindOrphaned,
        CheckPermissions,
    }

    public enum FileProcessSeverity
    {
        Info,
        Warn,
        Error
    }

    public class ProcessOutputReport
    {
        public string OperationTitle { get; set; }
        public List<ProcessOperation> Operations;

        public ProcessOutputReport()
        {
            Operations = new List<ProcessOperation>();
        }
    }

    public class ProcessOperation
    {
        public FileProcessSeverity Severity { get; set; }
        public ProcessRecommendedOutput Output { get; set; }
        public string Message { get; set; }
        public string SourceFilename { get; set; }
        public string DestinationFilename { get; set; }
    }
}
