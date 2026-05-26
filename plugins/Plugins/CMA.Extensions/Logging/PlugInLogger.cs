using CMA.Extensions.Logging;
using CMA.Extensions.Model;
using Microsoft.Crm.Sdk;
using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Runtime.Remoting.Services;
using System.Text;
using System.Threading.Tasks;

namespace CMA.Extensions
{
    public sealed class PlugInLogger
    {
        private ITracingService _tracingService;
        private IOrganizationService _organizationService;
        private IPluginExecutionContext7 _context;
        private List<PlugInRecord> _logEntries = new List<PlugInRecord>();
        private bool _LogTrace = false;
        private bool _LogInfo = false;
        private bool _LogWarning = false;
        private bool _LogError = true;

        private PlugInLogger() { }

        public PlugInLogger(IPluginExecutionContext7 context, ITracingService tracingService, IOrganizationService organizationService)
        {
            _context = context;
            _tracingService = tracingService;
            _organizationService = organizationService;
            GetTraceLevels();
        }

        private void GetTraceLevels()
        {
            //Should come from cma_configuration entity.
            _LogTrace = true;
            _LogInfo = true;
            _LogWarning = true;
            _LogError = true;
        }

        public void DumpContext()
        {
            // Access common Context properties
            Trace($"[Message] - {_context.MessageName}");
            Trace($"[Stage] - {_context.Stage}");
            Trace($"[Depth] - {_context.Depth}");
            Trace($"[UserId] - {_context.UserId}");
            Trace($"[InitiatingUserId] - {_context.InitiatingUserId}");
            Trace($"[InitiatingUserApplicationId] - {_context.InitiatingUserApplicationId}");
            Trace($"[IsApplicationUser] - {_context.IsApplicationUser}");
        }

        public void DumpEntity(Entity entity)
        {
            Trace($"[Entity Logical Name] - {entity.LogicalName}");
            Trace($"[Entity Id] - {entity.Id}");
        }

        // Finally, any singleton should define some business logic, which can
        // be executed on its instance.
        public void Info(string message)
        {
            PlugInRecord log = new PlugInRecord
            {
                Name = $"[{_context.MessageName}] - Information",
                Level = PlugInRecord.LoggingLevel.Information,
                RecordDate = DateTime.UtcNow.ToString("o"),
                Content = message
            };

            if (_LogInfo && _tracingService != null)
            {
                _logEntries.Add(log);
                _tracingService.Trace(log.ToString());
            }
        }

        public void Trace(string message)
        {
            PlugInRecord log = new PlugInRecord
            {
                Name = $"[{_context.MessageName}] - Trace",
                Level = PlugInRecord.LoggingLevel.Trace,
                RecordDate = DateTime.UtcNow.ToString("o"),
                Content = message
            };

            if (_LogTrace && _tracingService != null)
            {
                _logEntries.Add(log);
                _tracingService.Trace(message);
            }
        }

        public void Error(string message, Exception ex)
        {
            PlugInRecord log = new PlugInRecord
            {
                Name = $"[{_context.MessageName}] - Error",
                Level = PlugInRecord.LoggingLevel.Error,
                RecordDate = DateTime.UtcNow.ToString("o"),
                Content = $"{message} - Exception: {ex.Message}"
            };

            _logEntries.Add(log);

            if (_tracingService != null)
            {
                _tracingService.Trace(log.ToString());
            }
        }

        public void Warning(string message)
        {
            PlugInRecord log = new PlugInRecord
            {
                Name = $"[{_context.MessageName}] - Warning",
                Level = PlugInRecord.LoggingLevel.Warning,
                RecordDate = DateTime.UtcNow.ToString("o"),
                Content = message
            };

            if (_LogWarning && _tracingService != null) 
            { 
                _logEntries.Add(log);
                _tracingService.Trace(log.ToString());
            }
        }

        /// <summary>
        /// At the end of the execution, we persist the data to CRM for later storage.
        /// </summary>
        public void Persist()
        {
            foreach (var log in _logEntries)
            {
                new_CMADiagnosticLog persistedlog = new new_CMADiagnosticLog()
                {
                    new_Name = log.Name,
                    new_Content = log.Content,
                    new_Level = (new_cmadiagnosticlog_new_level)log.Level,
                    new_RecordDate = DateTime.Parse(log.RecordDate),
                    new_System = new_cmadiagnosticlog_new_system.Plugin,
                    new_UserId = _context.UserId.ToString()
                };

                _organizationService.Create(persistedlog.ToEntity<Entity>());
            }
        }
    }
}
