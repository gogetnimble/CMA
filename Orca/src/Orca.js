/**
 * Orca - A comprehensive wrapper around Dynamics 365 Xrm.WebApi, Xrm.Utility, and Xrm.Navigation
 * Provides enhanced functionality with built-in logging, notifications, and error handling
 * @version 2.0.0
 */

(function (window) {
    'use strict';

    /**
     * Logging levels
     */
    const LogLevel = {
        NONE: 0,
        ERROR: 1,
        WARN: 2,
        INFO: 3,
        VERBOSE: 4
    };

    /**
     * Notification types mapping to Xrm formContext notification levels
     */
    const NotificationType = {
        ERROR: 1,
        WARNING: 2,
        INFO: 3
    };

    /**
     * Alert dialog icon types
     */
    const AlertIcon = {
        ERROR: 'ERROR',
        WARNING: 'WARNING',
        INFO: 'INFO',
        SUCCESS: 'SUCCESS',
        QUESTION: 'QUESTION'
    };

    /**
     * Main Orca class
     */
    class Orca {
        constructor(options = {}) {
            // Default configuration
            this.logLevel = options.logLevel || LogLevel.INFO;
            this.autoReloadOnLogLevelChange = options.autoReloadOnLogLevelChange !== false;
            this.logToConsole = options.logToConsole !== false;
            this.logHistory = [];
            this.maxLogHistory = options.maxLogHistory || 100;
            
            // Remote logging configuration
            this.enableRemoteLogging = options.enableRemoteLogging || false;
            this.remoteLogOnlyErrorsWarnings = options.remoteLogOnlyErrorsWarnings !== false;
            this.remoteLogBatchSize = options.remoteLogBatchSize || 10;
            this.remoteLogQueue = [];
            this.remoteLogTimer = null;
            
            // Configuration loading
            this.configurationLoaded = false;
            this.loadConfigFromEntity = options.loadConfigFromEntity !== false;
            this.configEntityName = options.configEntityName || 'new_cmaconfiguration';
            
            // User context
            this.userId = null;
            this.userName = null;
            this.userEmail = null;

            // Verify Xrm is available
            if (typeof Xrm === 'undefined') {
                throw new Error('Xrm is not available. Ensure this script runs in a D365 context.');
            }

            // Get user context
            this._loadUserContext();

            // Initialize sub-modules with capitalized names
            this.Service = new ServiceWrapper(this);
            this.Utility = new UtilityWrapper(this);
            this.Notification = new NotificationWrapper(this);
            this.Log = new LogWrapper(this);
            
            // Load configuration from entity if enabled
            if (this.loadConfigFromEntity) {
                this._loadConfigurationFromEntity().catch(error => {
                    console.error('Failed to load configuration from entity:', error);
                    // Continue with default configuration
                });
            }
        }

        /**
         * Load user context from Xrm
         * @private
         */
        _loadUserContext() {
            try {
                const userSettings = Xrm.Utility.getGlobalContext().userSettings;
                this.userId = userSettings.userId.replace(/[{}]/g, '');
                this.userName = userSettings.userName;
                
                // Try to get email if available
                try {
                    const user = Xrm.Utility.getGlobalContext().getCurrentAppUser();
                    if (user && user.email) {
                        this.userEmail = user.email;
                    }
                } catch (e) {
                    // Email not available in some contexts
                }
                
                this.log(LogLevel.VERBOSE, 'Orca', 'User context loaded', {
                    userId: this.userId,
                    userName: this.userName
                });
            } catch (error) {
                console.error('Failed to load user context:', error);
            }
        }

        /**
         * Load configuration from new_cmaconfiguration entity
         * @private
         */
        async _loadConfigurationFromEntity() {
            try {
                this.log(LogLevel.VERBOSE, 'Orca', 'Loading configuration from entity');
                
                // Query for active configuration
                // Assumes there's only one active configuration record
                const options = `?$select=new_loglevel,new_autoreloadonloglevelchange,new_logtoconsole,new_maxloghistory,new_enableremotelogging,new_remotelogonlyerrorswarnings,new_remotelogbatchsize&$filter=statecode eq 0&$top=1`;
                
                const result = await Xrm.WebApi.retrieveMultipleRecords(this.configEntityName, options);
                
                if (result.entities.length > 0) {
                    const config = result.entities[0];
                    
                    // Apply configuration
                    if (config.new_loglevel !== null && config.new_loglevel !== undefined) {
                        this.logLevel = config.new_loglevel;
                    }
                    
                    if (config.new_autoreloadonloglevelchange !== null && config.new_autoreloadonloglevelchange !== undefined) {
                        this.autoReloadOnLogLevelChange = config.new_autoreloadonloglevelchange;
                    }
                    
                    if (config.new_logtoconsole !== null && config.new_logtoconsole !== undefined) {
                        this.logToConsole = config.new_logtoconsole;
                    }
                    
                    if (config.new_maxloghistory !== null && config.new_maxloghistory !== undefined) {
                        this.maxLogHistory = config.new_maxloghistory;
                    }
                    
                    if (config.new_enableremotelogging !== null && config.new_enableremotelogging !== undefined) {
                        this.enableRemoteLogging = config.new_enableremotelogging;
                    }
                    
                    if (config.new_remotelogonlyerrorswarnings !== null && config.new_remotelogonlyerrorswarnings !== undefined) {
                        this.remoteLogOnlyErrorsWarnings = config.new_remotelogonlyerrorswarnings;
                    }
                    
                    if (config.new_remotelogbatchsize !== null && config.new_remotelogbatchsize !== undefined) {
                        this.remoteLogBatchSize = config.new_remotelogbatchsize;
                    }
                    
                    this.configurationLoaded = true;
                    
                    this.log(LogLevel.INFO, 'Orca', 'Configuration loaded from entity', {
                        logLevel: this._getLogLevelName(this.logLevel),
                        enableRemoteLogging: this.enableRemoteLogging
                    });
                } else {
                    this.log(LogLevel.WARN, 'Orca', 'No active configuration found in entity, using defaults');
                }
            } catch (error) {
                this.log(LogLevel.ERROR, 'Orca', 'Failed to load configuration from entity', error);
                throw error;
            }
        }

        /**
         * Manually reload configuration from entity
         */
        async reloadConfiguration() {
            await this._loadConfigurationFromEntity();
            return this.configurationLoaded;
        }

        /**
         * Set the global logging level
         * @param {number} level - LogLevel constant
         * @param {boolean} reload - Whether to reload the page after changing log level
         */
        setLogLevel(level, reload = null) {
            const shouldReload = reload !== null ? reload : this.autoReloadOnLogLevelChange;
            this.logLevel = level;

            this.log(LogLevel.INFO, 'Orca', `Log level changed to: ${this._getLogLevelName(level)}`);

            if (shouldReload && typeof Xrm.Navigation !== 'undefined') {
                this.log(LogLevel.INFO, 'Orca', 'Reloading page due to log level change...');
                setTimeout(() => {
                    Xrm.Navigation.openAlertDialog({
                        text: 'Log level changed. Reloading page...',
                        confirmButtonLabel: 'OK'
                    }).then(() => {
                        window.location.reload();
                    });
                }, 500);
            }
        }

        /**
         * Get current log level
         */
        getLogLevel() {
            return this.logLevel;
        }

        /**
         * Core logging method
         * @param {number} level - LogLevel constant
         * @param {string} component - Component/module name
         * @param {string} message - Log message
         * @param {*} data - Additional data to log
         */
        log(level, component, message, data = null) {
            if (level > this.logLevel) return;

            const timestamp = new Date().toISOString();
            const logEntry = {
                timestamp,
                level: this._getLogLevelName(level),
                component,
                message,
                data
            };

            // Add to history
            this.logHistory.push(logEntry);
            if (this.logHistory.length > this.maxLogHistory) {
                this.logHistory.shift();
            }

            // Console logging
            if (this.logToConsole) {
                const logMethod = this._getConsoleMethod(level);
                const logMessage = `[${timestamp}] [${logEntry.level}] [${component}] ${message}`;
                
                if (data) {
                    console[logMethod](logMessage, data);
                } else {
                    console[logMethod](logMessage);
                }
            }

            // Remote logging
            if (this.enableRemoteLogging) {
                const shouldLogRemotely = !this.remoteLogOnlyErrorsWarnings || 
                                         (level === LogLevel.ERROR || level === LogLevel.WARN);
                
                if (shouldLogRemotely) {
                    this._queueRemoteLog(logEntry);
                }
            }
        }

        /**
         * Queue a log entry for remote logging
         * @private
         */
        _queueRemoteLog(logEntry) {
            // Add user context to log entry
            const remoteLogEntry = {
                ...logEntry,
                userId: this.userId,
                userName: this.userName,
                userEmail: this.userEmail,
                url: window.location.href,
                userAgent: navigator.userAgent
            };

            this.remoteLogQueue.push(remoteLogEntry);

            // If batch size reached, flush immediately
            if (this.remoteLogQueue.length >= this.remoteLogBatchSize) {
                this._flushRemoteLogs();
            } else {
                // Otherwise, schedule a flush
                this._scheduleRemoteLogFlush();
            }
        }

        /**
         * Schedule a remote log flush
         * @private
         */
        _scheduleRemoteLogFlush() {
            if (this.remoteLogTimer) {
                return; // Already scheduled
            }

            // Flush after 5 seconds of inactivity
            this.remoteLogTimer = setTimeout(() => {
                this._flushRemoteLogs();
            }, 5000);
        }

        /**
         * Flush remote logs to D365
         * @private
         */
        async _flushRemoteLogs() {
            if (this.remoteLogTimer) {
                clearTimeout(this.remoteLogTimer);
                this.remoteLogTimer = null;
            }

            if (this.remoteLogQueue.length === 0) {
                return;
            }

            const logsToSend = [...this.remoteLogQueue];
            this.remoteLogQueue = [];

            try {
                // Create log records in batches
                const promises = logsToSend.map(logEntry => {
                    const logRecord = {
                        'new_name': `${logEntry.level} - ${logEntry.component} - ${logEntry.timestamp}`,
                        'new_level': this._mapLogLevelToOptionSet(logEntry.level),
                        'new_component': logEntry.component,
                        'new_message': logEntry.message,
                        'new_timestamp': logEntry.timestamp,
                        'new_data': logEntry.data ? JSON.stringify(logEntry.data, null, 2) : null,
                        'new_url': logEntry.url,
                        'new_useragent': logEntry.userAgent,
                        'new_userid@odata.bind': `/systemusers(${logEntry.userId})`
                    };

                    return Xrm.WebApi.createRecord('new_cmajavascriptlog', logRecord)
                        .catch(error => {
                            // Log to console if remote logging fails
                            console.error('Failed to create remote log record:', error, logEntry);
                        });
                });

                await Promise.all(promises);

                // Only log success at verbose level to avoid recursion
                if (this.logLevel >= LogLevel.VERBOSE) {
                    console.verbose(`[Orca] Successfully logged ${logsToSend.length} entries remotely`);
                }
            } catch (error) {
                console.error('Failed to flush remote logs:', error);
                // Re-queue failed logs
                this.remoteLogQueue.unshift(...logsToSend);
            }
        }

        /**
         * Map log level string to option set value
         * @private
         */
        _mapLogLevelToOptionSet(levelName) {
            const mapping = {
                'ERROR': 1,
                'WARN': 2,
                'INFO': 3,
                'VERBOSE': 4,
                'NONE': 0
            };
            return mapping[levelName] || 3;
        }

        /**
         * Manually flush remote logs
         */
        async flushRemoteLogs() {
            await this._flushRemoteLogs();
        }

        /**
         * Log info message
         */
        info(component, message, data = null) {
            this.log(LogLevel.INFO, component, message, data);
        }

        /**
         * Log warning message
         */
        warn(component, message, data = null) {
            this.log(LogLevel.WARN, component, message, data);
        }

        /**
         * Log error message
         */
        error(component, message, data = null) {
            this.log(LogLevel.ERROR, component, message, data);
        }

        /**
         * Log verbose message
         */
        verbose(component, message, data = null) {
            this.log(LogLevel.VERBOSE, component, message, data);
        }

        /**
         * Get log history
         */
        getLogHistory(level = null) {
            if (level === null) return [...this.logHistory];
            
            const levelName = this._getLogLevelName(level);
            return this.logHistory.filter(entry => entry.level === levelName);
        }

        /**
         * Clear log history
         */
        clearLogHistory() {
            this.logHistory = [];
            this.info('Orca', 'Log history cleared');
        }

        /**
         * Export logs as JSON string
         */
        exportLogs() {
            return JSON.stringify(this.logHistory, null, 2);
        }

        /**
         * Get log level name from constant
         * @private
         */
        _getLogLevelName(level) {
            const levelNames = {
                [LogLevel.NONE]: 'NONE',
                [LogLevel.ERROR]: 'ERROR',
                [LogLevel.WARN]: 'WARN',
                [LogLevel.INFO]: 'INFO',
                [LogLevel.VERBOSE]: 'VERBOSE'
            };
            return levelNames[level] || 'UNKNOWN';
        }

        /**
         * Get appropriate console method for log level
         * @private
         */
        _getConsoleMethod(level) {
            switch (level) {
                case LogLevel.ERROR: return 'error';
                case LogLevel.WARN: return 'warn';
                case LogLevel.VERBOSE: return 'debug';  // Use console.debug for verbose
                default: return 'log';
            }
        }
    }

    /**
     * Log Wrapper - Provides capitalized logging methods
     */
    class LogWrapper {
        constructor(client) {
            this.client = client;
        }

        /**
         * Log info message
         */
        Info(component, message, data = null) {
            this.client.info(component, message, data);
        }

        /**
         * Log warning message
         */
        Warn(component, message, data = null) {
            this.client.warn(component, message, data);
        }

        /**
         * Log error message
         */
        Error(component, message, data = null) {
            this.client.error(component, message, data);
        }

        /**
         * Log verbose message
         */
        Verbose(component, message, data = null) {
            this.client.verbose(component, message, data);
        }
    }

    /**
     * Service Wrapper (WebAPI)
     */
    class ServiceWrapper {
        constructor(client) {
            this.client = client;
        }

        /**
         * Create a record
         * @param {string} entityLogicalName - Logical name of the entity
         * @param {object} data - Record data
         * @returns {Promise}
         */
        async createRecord(entityLogicalName, data) {
            this.client.info('WebApi', `Creating ${entityLogicalName} record`, data);
            
            try {
                const result = await Xrm.WebApi.createRecord(entityLogicalName, data);
                this.client.info('WebApi', `Successfully created ${entityLogicalName}`, result);
                return result;
            } catch (error) {
                this.client.error('WebApi', `Failed to create ${entityLogicalName}`, error);
                throw error;
            }
        }

        /**
         * Retrieve a record
         * @param {string} entityLogicalName - Logical name of the entity
         * @param {string} id - GUID of the record
         * @param {string} options - OData query options
         * @returns {Promise}
         */
        async retrieveRecord(entityLogicalName, id, options) {
            this.client.verbose('WebApi', `Retrieving ${entityLogicalName} record: ${id}`, options);
            
            try {
                const result = await Xrm.WebApi.retrieveRecord(entityLogicalName, id, options);
                this.client.verbose('WebApi', `Successfully retrieved ${entityLogicalName}`, result);
                return result;
            } catch (error) {
                this.client.error('WebApi', `Failed to retrieve ${entityLogicalName}: ${id}`, error);
                throw error;
            }
        }

        /**
         * Retrieve multiple records
         * @param {string} entityLogicalName - Logical name of the entity
         * @param {string} options - OData query options
         * @param {number} maxPageSize - Max page size (default 5000)
         * @returns {Promise}
         */
        async retrieveMultipleRecords(entityLogicalName, options, maxPageSize) {
            this.client.verbose('WebApi', `Retrieving multiple ${entityLogicalName} records`, { options, maxPageSize });
            
            try {
                const result = await Xrm.WebApi.retrieveMultipleRecords(entityLogicalName, options, maxPageSize);
                this.client.verbose('WebApi', `Successfully retrieved ${result.entities.length} ${entityLogicalName} records`);
                return result;
            } catch (error) {
                this.client.error('WebApi', `Failed to retrieve multiple ${entityLogicalName} records`, error);
                throw error;
            }
        }

        /**
         * Update a record
         * @param {string} entityLogicalName - Logical name of the entity
         * @param {string} id - GUID of the record
         * @param {object} data - Updated data
         * @returns {Promise}
         */
        async updateRecord(entityLogicalName, id, data) {
            this.client.info('WebApi', `Updating ${entityLogicalName} record: ${id}`, data);
            
            try {
                const result = await Xrm.WebApi.updateRecord(entityLogicalName, id, data);
                this.client.info('WebApi', `Successfully updated ${entityLogicalName}: ${id}`);
                return result;
            } catch (error) {
                this.client.error('WebApi', `Failed to update ${entityLogicalName}: ${id}`, error);
                throw error;
            }
        }

        /**
         * Delete a record
         * @param {string} entityLogicalName - Logical name of the entity
         * @param {string} id - GUID of the record
         * @returns {Promise}
         */
        async deleteRecord(entityLogicalName, id) {
            this.client.info('WebApi', `Deleting ${entityLogicalName} record: ${id}`);
            
            try {
                const result = await Xrm.WebApi.deleteRecord(entityLogicalName, id);
                this.client.info('WebApi', `Successfully deleted ${entityLogicalName}: ${id}`);
                return result;
            } catch (error) {
                this.client.error('WebApi', `Failed to delete ${entityLogicalName}: ${id}`, error);
                throw error;
            }
        }

        /**
         * Execute a Web API request
         * @param {string} request - Request URI
         * @returns {Promise}
         */
        async execute(request) {
            this.client.verbose('WebApi', 'Executing Web API request', request);
            
            try {
                const result = await Xrm.WebApi.execute(request);
                this.client.verbose('WebApi', 'Web API request executed successfully');
                return result;
            } catch (error) {
                this.client.error('WebApi', 'Web API request failed', error);
                throw error;
            }
        }

        /**
         * Execute multiple Web API requests
         * @param {Array} requests - Array of requests
         * @returns {Promise}
         */
        async executeMultiple(requests) {
            this.client.verbose('WebApi', `Executing ${requests.length} Web API requests`);
            
            try {
                const result = await Xrm.WebApi.executeMultiple(requests);
                this.client.verbose('WebApi', 'Multiple Web API requests executed successfully');
                return result;
            } catch (error) {
                this.client.error('WebApi', 'Multiple Web API requests failed', error);
                throw error;
            }
        }

        /**
         * Check if a record is available offline
         * @param {string} entityLogicalName - Logical name of the entity
         * @param {string} id - GUID of the record
         * @returns {Promise<boolean>}
         */
        async isAvailableOffline(entityLogicalName, id) {
            this.client.verbose('WebApi', `Checking offline availability for ${entityLogicalName}: ${id}`);
            
            try {
                const result = await Xrm.WebApi.offline.isAvailableOffline(entityLogicalName, id);
                this.client.verbose('WebApi', `Offline availability: ${result}`);
                return result;
            } catch (error) {
                this.client.error('WebApi', 'Failed to check offline availability', error);
                throw error;
            }
        }
    }

    /**
     * Utility Wrapper
     */
    class UtilityWrapper {
        constructor(client) {
            this.client = client;
        }

        /**
         * Close a progress indicator
         */
        closeProgressIndicator() {
            this.client.verbose('Utility', 'Closing progress indicator');
            Xrm.Utility.closeProgressIndicator();
        }

        /**
         * Get global context
         */
        getGlobalContext() {
            this.client.verbose('Utility', 'Getting global context');
            return Xrm.Utility.getGlobalContext();
        }

        /**
         * Get learning path attribution
         */
        getLearningPathAttributionSync() {
            return Xrm.Utility.getLearningPathAttributionSync();
        }

        /**
         * Get entity metadata
         * @param {string} entityName - Entity logical name
         * @param {Array<string>} attributes - Attribute names
         * @returns {Promise}
         */
        async getEntityMetadata(entityName, attributes) {
            this.client.verbose('Utility', `Getting metadata for ${entityName}`, attributes);
            
            try {
                const result = await Xrm.Utility.getEntityMetadata(entityName, attributes);
                this.client.verbose('Utility', `Successfully retrieved metadata for ${entityName}`);
                return result;
            } catch (error) {
                this.client.error('Utility', `Failed to get metadata for ${entityName}`, error);
                throw error;
            }
        }

        /**
         * Get resource string
         * @param {string} webResourceName - Web resource name
         * @param {string} key - Resource key
         * @returns {string}
         */
        getResourceString(webResourceName, key) {
            this.client.verbose('Utility', `Getting resource string: ${webResourceName}.${key}`);
            return Xrm.Utility.getResourceString(webResourceName, key);
        }

        /**
         * Invoke a process action
         * @param {string} name - Action name
         * @param {object} parameters - Action parameters
         * @returns {Promise}
         */
        async invokeProcessAction(name, parameters) {
            this.client.info('Utility', `Invoking process action: ${name}`, parameters);
            
            try {
                const result = await Xrm.Utility.invokeProcessAction(name, parameters);
                this.client.info('Utility', `Successfully invoked process action: ${name}`);
                return result;
            } catch (error) {
                this.client.error('Utility', `Failed to invoke process action: ${name}`, error);
                throw error;
            }
        }

        /**
         * Look up objects
         * @param {object} lookupOptions - Lookup options
         * @returns {Promise}
         */
        async lookupObjects(lookupOptions) {
            this.client.verbose('Utility', 'Opening lookup dialog', lookupOptions);
            
            try {
                const result = await Xrm.Utility.lookupObjects(lookupOptions);
                this.client.verbose('Utility', `Lookup returned ${result.length} results`);
                return result;
            } catch (error) {
                this.client.error('Utility', 'Lookup failed', error);
                throw error;
            }
        }

        /**
         * Refresh parent grid
         */
        refreshParentGrid(lookupOptions) {
            this.client.verbose('Utility', 'Refreshing parent grid', lookupOptions);
            Xrm.Utility.refreshParentGrid(lookupOptions);
        }

        /**
         * Show progress indicator
         * @param {string} message - Progress message
         */
        showProgressIndicator(message) {
            this.client.verbose('Utility', `Showing progress indicator: ${message}`);
            Xrm.Utility.showProgressIndicator(message);
        }

        /**
         * Get page context
         */
        getPageContext() {
            return Xrm.Utility.getPageContext();
        }

        /**
         * Open alert dialog
         * @param {string} text - Alert text
         * @param {object} options - Additional options
         * @returns {Promise}
         */
        async showAlert(text, options = {}) {
            const alertOptions = {
                text: text,
                confirmButtonLabel: options.confirmButtonLabel || 'OK'
            };

            this.client.verbose('Utility', 'Showing alert dialog', alertOptions);
            
            try {
                await Xrm.Navigation.openAlertDialog(alertOptions);
                this.client.verbose('Utility', 'Alert dialog closed');
            } catch (error) {
                this.client.error('Utility', 'Failed to show alert', error);
                throw error;
            }
        }

        /**
         * Open confirmation dialog
         * @param {string} text - Confirmation text
         * @param {object} options - Additional options
         * @returns {Promise<boolean>}
         */
        async showConfirm(text, options = {}) {
            const confirmOptions = {
                text: text,
                title: options.title || 'Confirm',
                confirmButtonLabel: options.confirmButtonLabel || 'Yes',
                cancelButtonLabel: options.cancelButtonLabel || 'No'
            };

            this.client.verbose('Utility', 'Showing confirmation dialog', confirmOptions);
            
            try {
                const result = await Xrm.Navigation.openConfirmDialog(confirmOptions);
                this.client.verbose('Utility', `Confirmation result: ${result.confirmed}`);
                return result.confirmed;
            } catch (error) {
                this.client.error('Utility', 'Failed to show confirmation', error);
                throw error;
            }
        }

        /**
         * Open error dialog
         * @param {object} errorOptions - Error dialog options
         * @returns {Promise}
         */
        async showErrorDialog(errorOptions) {
            this.client.verbose('Utility', 'Showing error dialog', errorOptions);
            
            try {
                await Xrm.Navigation.openErrorDialog(errorOptions);
                this.client.verbose('Utility', 'Error dialog closed');
            } catch (error) {
                this.client.error('Utility', 'Failed to show error dialog', error);
                throw error;
            }
        }

        /**
         * Open a file
         * @param {object} file - File object
         * @param {object} options - Open options
         * @returns {Promise}
         */
        async openFile(file, options = {}) {
            this.client.verbose('Utility', 'Opening file', { file, options });
            
            try {
                await Xrm.Navigation.openFile(file, options);
                this.client.verbose('Utility', 'File opened successfully');
            } catch (error) {
                this.client.error('Utility', 'Failed to open file', error);
                throw error;
            }
        }

        /**
         * Open a form
         * @param {object} entityFormOptions - Form options
         * @param {object} formParameters - Form parameters
         * @returns {Promise}
         */
        async openForm(entityFormOptions, formParameters) {
            this.client.verbose('Utility', 'Opening form', { entityFormOptions, formParameters });
            
            try {
                const result = await Xrm.Navigation.openForm(entityFormOptions, formParameters);
                this.client.verbose('Utility', 'Form opened successfully');
                return result;
            } catch (error) {
                this.client.error('Utility', 'Failed to open form', error);
                throw error;
            }
        }

        /**
         * Open a URL
         * @param {string} url - URL to open
         * @param {object} options - Open options
         */
        openUrl(url, options = {}) {
            this.client.verbose('Utility', `Opening URL: ${url}`, options);
            
            try {
                Xrm.Navigation.openUrl(url, options);
                this.client.verbose('Utility', 'URL opened successfully');
            } catch (error) {
                this.client.error('Utility', 'Failed to open URL', error);
                throw error;
            }
        }

        /**
         * Open web resource
         * @param {string} webResourceName - Web resource name
         * @param {object} options - Open options
         * @param {string} data - Data to pass
         * @returns {Promise}
         */
        async openWebResource(webResourceName, options, data) {
            this.client.verbose('Utility', `Opening web resource: ${webResourceName}`, { options, data });
            
            try {
                const result = await Xrm.Navigation.openWebResource(webResourceName, options, data);
                this.client.verbose('Utility', 'Web resource opened successfully');
                return result;
            } catch (error) {
                this.client.error('Utility', 'Failed to open web resource', error);
                throw error;
            }
        }
    }

    /**
     * Notification Wrapper
     */
    class NotificationWrapper {
        constructor(client) {
            this.client = client;
        }

        /**
         * Set a form notification
         * @param {object} formContext - Form context
         * @param {string} message - Notification message
         * @param {string} level - Notification level (ERROR, WARNING, INFO)
         * @param {string} uniqueId - Unique identifier for the notification
         * @returns {boolean}
         */
        setFormNotification(formContext, message, level = 'INFO', uniqueId = null) {
            uniqueId = uniqueId || this._generateUniqueId();
            const notificationLevel = NotificationType[level] || NotificationType.INFO;

            this.client.verbose('Notification', `Setting form notification (${level}): ${message}`, { uniqueId });
            
            try {
                const result = formContext.ui.setFormNotification(message, notificationLevel, uniqueId);
                this.client.verbose('Notification', 'Form notification set successfully');
                return result;
            } catch (error) {
                this.client.error('Notification', 'Failed to set form notification', error);
                return false;
            }
        }

        /**
         * Clear a form notification
         * @param {object} formContext - Form context
         * @param {string} uniqueId - Unique identifier for the notification
         * @returns {boolean}
         */
        clearFormNotification(formContext, uniqueId) {
            this.client.verbose('Notification', `Clearing form notification: ${uniqueId}`);
            
            try {
                const result = formContext.ui.clearFormNotification(uniqueId);
                this.client.verbose('Notification', 'Form notification cleared successfully');
                return result;
            } catch (error) {
                this.client.error('Notification', 'Failed to clear form notification', error);
                return false;
            }
        }

        /**
         * Set an attribute notification
         * @param {object} attribute - Attribute control
         * @param {string} message - Notification message
         * @param {string} uniqueId - Unique identifier
         * @returns {boolean}
         */
        setAttributeNotification(attribute, message, uniqueId = null) {
            uniqueId = uniqueId || this._generateUniqueId();
            
            this.client.verbose('Notification', `Setting attribute notification: ${message}`, { uniqueId });
            
            try {
                const result = attribute.controls.forEach(control => {
                    control.setNotification(message, uniqueId);
                });
                this.client.verbose('Notification', 'Attribute notification set successfully');
                return true;
            } catch (error) {
                this.client.error('Notification', 'Failed to set attribute notification', error);
                return false;
            }
        }

        /**
         * Clear an attribute notification
         * @param {object} attribute - Attribute control
         * @param {string} uniqueId - Unique identifier
         * @returns {boolean}
         */
        clearAttributeNotification(attribute, uniqueId) {
            this.client.verbose('Notification', `Clearing attribute notification: ${uniqueId}`);
            
            try {
                attribute.controls.forEach(control => {
                    control.clearNotification(uniqueId);
                });
                this.client.verbose('Notification', 'Attribute notification cleared successfully');
                return true;
            } catch (error) {
                this.client.error('Notification', 'Failed to clear attribute notification', error);
                return false;
            }
        }

        /**
         * Show an alert notification using Xrm.Navigation
         * @param {string} message - Alert message
         * @param {string} title - Alert title
         * @param {string} icon - Alert icon type
         * @returns {Promise}
         */
        async showAlert(message, title = 'Alert', icon = null) {
            const alertOptions = {
                text: message,
                title: title
            };

            if (icon && AlertIcon[icon]) {
                alertOptions.icon = icon;
            }

            this.client.verbose('Notification', `Showing alert: ${title}`, alertOptions);
            
            try {
                await Xrm.Navigation.openAlertDialog(alertOptions);
                this.client.verbose('Notification', 'Alert closed');
            } catch (error) {
                this.client.error('Notification', 'Failed to show alert', error);
                throw error;
            }
        }

        /**
         * Show an error notification
         * @param {string} message - Error message
         * @param {object} errorDetails - Additional error details
         * @returns {Promise}
         */
        async showError(message, errorDetails = null) {
            const errorOptions = {
                message: message
            };

            if (errorDetails) {
                errorOptions.details = typeof errorDetails === 'string' ? errorDetails : JSON.stringify(errorDetails);
            }

            this.client.error('Notification', `Showing error: ${message}`, errorDetails);
            
            try {
                await Xrm.Navigation.openErrorDialog(errorOptions);
                this.client.verbose('Notification', 'Error dialog closed');
            } catch (error) {
                this.client.error('Notification', 'Failed to show error dialog', error);
                throw error;
            }
        }

        /**
         * Show a confirmation dialog
         * @param {string} message - Confirmation message
         * @param {string} title - Dialog title
         * @returns {Promise<boolean>}
         */
        async showConfirm(message, title = 'Confirm') {
            const confirmOptions = {
                text: message,
                title: title
            };

            this.client.verbose('Notification', `Showing confirmation: ${title}`);
            
            try {
                const result = await Xrm.Navigation.openConfirmDialog(confirmOptions);
                this.client.verbose('Notification', `Confirmation result: ${result.confirmed}`);
                return result.confirmed;
            } catch (error) {
                this.client.error('Notification', 'Failed to show confirmation', error);
                throw error;
            }
        }

        /**
         * Generate a unique ID for notifications
         * @private
         */
        _generateUniqueId() {
            return `notification_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`;
        }
    }

    // Export to global scope
    window.Orca = Orca;
    window.Orca.LogLevel = LogLevel;
    window.Orca.NotificationType = NotificationType;
    window.Orca.AlertIcon = AlertIcon;

})(window);
