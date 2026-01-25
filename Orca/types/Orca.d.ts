/**
 * TypeScript definitions for D365Client
 * Provides IntelliSense and type checking for the D365Client library
 */

declare global {
    interface Window {
        D365Client: typeof D365Client;
    }
}

/**
 * Log level enumeration
 */
export enum LogLevel {
    NONE = 0,
    ERROR = 1,
    WARN = 2,
    INFO = 3,
    DEBUG = 4
}

/**
 * Notification type enumeration
 */
export enum NotificationType {
    ERROR = 1,
    WARNING = 2,
    INFO = 3
}

/**
 * Alert icon enumeration
 */
export enum AlertIcon {
    ERROR = 'ERROR',
    WARNING = 'WARNING',
    INFO = 'INFO',
    SUCCESS = 'SUCCESS',
    QUESTION = 'QUESTION'
}

/**
 * D365Client initialization options
 */
export interface D365ClientOptions {
    /** Current log level (default: LogLevel.INFO) */
    logLevel?: LogLevel;
    /** Whether to auto-reload page when log level changes (default: true) */
    autoReloadOnLogLevelChange?: boolean;
    /** Whether to log to browser console (default: true) */
    logToConsole?: boolean;
    /** Maximum number of log entries to keep (default: 100) */
    maxLogHistory?: number;
    /** Enable remote logging to D365 entity (default: false) */
    enableRemoteLogging?: boolean;
    /** Only log errors and warnings remotely (default: true) */
    remoteLogOnlyErrorsWarnings?: boolean;
    /** Number of logs to batch before sending to D365 (default: 10) */
    remoteLogBatchSize?: number;
    /** Load configuration from D365 entity (default: true) */
    loadConfigFromEntity?: boolean;
    /** Configuration entity name (default: 'new_cmaconfiguration') */
    configEntityName?: string;
}

/**
 * Log entry structure
 */
export interface LogEntry {
    timestamp: string;
    level: string;
    component: string;
    message: string;
    data: any;
}

/**
 * Alert dialog options
 */
export interface AlertOptions {
    confirmButtonLabel?: string;
}

/**
 * Confirmation dialog options
 */
export interface ConfirmOptions {
    title?: string;
    confirmButtonLabel?: string;
    cancelButtonLabel?: string;
}

/**
 * Entity form options for navigation
 */
export interface EntityFormOptions {
    entityName: string;
    entityId?: string;
    formId?: string;
    openInNewWindow?: boolean;
    useQuickCreateForm?: boolean;
    windowPosition?: number;
}

/**
 * Lookup options
 */
export interface LookupOptions {
    entityTypes: string[];
    allowMultiSelect?: boolean;
    defaultEntityType?: string;
    defaultViewId?: string;
    viewIds?: string[];
    searchText?: string;
}

/**
 * Web API retrieve result
 */
export interface RetrieveMultipleResult {
    entities: any[];
    nextLink?: string;
}

/**
 * Web API create result
 */
export interface CreateResult {
    id: string;
    entityType: string;
}

/**
 * WebAPI Wrapper Class
 */
export declare class WebApiWrapper {
    constructor(client: D365Client);

    /**
     * Create a new record
     */
    createRecord(entityLogicalName: string, data: any): Promise<CreateResult>;

    /**
     * Retrieve a single record
     */
    retrieveRecord(entityLogicalName: string, id: string, options?: string): Promise<any>;

    /**
     * Retrieve multiple records
     */
    retrieveMultipleRecords(
        entityLogicalName: string,
        options?: string,
        maxPageSize?: number
    ): Promise<RetrieveMultipleResult>;

    /**
     * Update a record
     */
    updateRecord(entityLogicalName: string, id: string, data: any): Promise<any>;

    /**
     * Delete a record
     */
    deleteRecord(entityLogicalName: string, id: string): Promise<any>;

    /**
     * Execute a Web API request
     */
    execute(request: any): Promise<any>;

    /**
     * Execute multiple Web API requests
     */
    executeMultiple(requests: any[]): Promise<any>;

    /**
     * Check if a record is available offline
     */
    isAvailableOffline(entityLogicalName: string, id: string): Promise<boolean>;
}

/**
 * Utility Wrapper Class
 */
export declare class UtilityWrapper {
    constructor(client: D365Client);

    /**
     * Close progress indicator
     */
    closeProgressIndicator(): void;

    /**
     * Get global context
     */
    getGlobalContext(): Xrm.GlobalContext;

    /**
     * Get learning path attribution
     */
    getLearningPathAttributionSync(): any;

    /**
     * Get entity metadata
     */
    getEntityMetadata(entityName: string, attributes?: string[]): Promise<any>;

    /**
     * Get resource string
     */
    getResourceString(webResourceName: string, key: string): string;

    /**
     * Invoke a process action
     */
    invokeProcessAction(name: string, parameters: any): Promise<any>;

    /**
     * Open lookup dialog
     */
    lookupObjects(lookupOptions: LookupOptions): Promise<any[]>;

    /**
     * Refresh parent grid
     */
    refreshParentGrid(lookupOptions?: any): void;

    /**
     * Show progress indicator
     */
    showProgressIndicator(message: string): void;

    /**
     * Get page context
     */
    getPageContext(): any;

    /**
     * Show alert dialog
     */
    showAlert(text: string, options?: AlertOptions): Promise<void>;

    /**
     * Show confirmation dialog
     */
    showConfirm(text: string, options?: ConfirmOptions): Promise<boolean>;

    /**
     * Show error dialog
     */
    showErrorDialog(errorOptions: any): Promise<void>;

    /**
     * Open a file
     */
    openFile(file: any, options?: any): Promise<void>;

    /**
     * Open a form
     */
    openForm(entityFormOptions: EntityFormOptions, formParameters?: any): Promise<any>;

    /**
     * Open a URL
     */
    openUrl(url: string, options?: any): void;

    /**
     * Open a web resource
     */
    openWebResource(webResourceName: string, options?: any, data?: string): Promise<any>;
}

/**
 * Notification Wrapper Class
 */
export declare class NotificationWrapper {
    constructor(client: D365Client);

    /**
     * Set a form notification
     */
    setFormNotification(
        formContext: Xrm.FormContext,
        message: string,
        level?: 'ERROR' | 'WARNING' | 'INFO',
        uniqueId?: string
    ): boolean;

    /**
     * Clear a form notification
     */
    clearFormNotification(formContext: Xrm.FormContext, uniqueId: string): boolean;

    /**
     * Set an attribute notification
     */
    setAttributeNotification(
        attribute: Xrm.Attributes.Attribute,
        message: string,
        uniqueId?: string
    ): boolean;

    /**
     * Clear an attribute notification
     */
    clearAttributeNotification(
        attribute: Xrm.Attributes.Attribute,
        uniqueId: string
    ): boolean;

    /**
     * Show an alert notification
     */
    showAlert(message: string, title?: string, icon?: string): Promise<void>;

    /**
     * Show an error notification
     */
    showError(message: string, errorDetails?: any): Promise<void>;

    /**
     * Show a confirmation dialog
     */
    showConfirm(message: string, title?: string): Promise<boolean>;
}

/**
 * Main D365Client class
 */
export declare class D365Client {
    /** WebAPI wrapper instance */
    webApi: WebApiWrapper;
    
    /** Utility wrapper instance */
    utility: UtilityWrapper;
    
    /** Notification wrapper instance */
    notification: NotificationWrapper;

    /** Current log level */
    logLevel: LogLevel;

    /** Whether to auto-reload on log level change */
    autoReloadOnLogLevelChange: boolean;

    /** Whether to log to console */
    logToConsole: boolean;

    /** Log history array */
    logHistory: LogEntry[];

    /** Maximum log history size */
    maxLogHistory: number;

    /** Enable remote logging to D365 */
    enableRemoteLogging: boolean;

    /** Only log errors and warnings remotely */
    remoteLogOnlyErrorsWarnings: boolean;

    /** Batch size for remote logging */
    remoteLogBatchSize: number;

    /** Configuration loaded from entity */
    configurationLoaded: boolean;

    /** Load configuration from entity */
    loadConfigFromEntity: boolean;

    /** Configuration entity name */
    configEntityName: string;

    /** Current user ID */
    userId: string | null;

    /** Current user name */
    userName: string | null;

    /** Current user email */
    userEmail: string | null;

    /**
     * Create a new D365Client instance
     */
    constructor(options?: D365ClientOptions);

    /**
     * Set the global logging level
     */
    setLogLevel(level: LogLevel, reload?: boolean): void;

    /**
     * Get the current log level
     */
    getLogLevel(): LogLevel;

    /**
     * Core logging method
     */
    log(level: LogLevel, component: string, message: string, data?: any): void;

    /**
     * Log an info message
     */
    info(component: string, message: string, data?: any): void;

    /**
     * Log a warning message
     */
    warn(component: string, message: string, data?: any): void;

    /**
     * Log an error message
     */
    error(component: string, message: string, data?: any): void;

    /**
     * Log a debug message
     */
    debug(component: string, message: string, data?: any): void;

    /**
     * Get log history
     */
    getLogHistory(level?: LogLevel): LogEntry[];

    /**
     * Clear log history
     */
    clearLogHistory(): void;

    /**
     * Export logs as JSON string
     */
    exportLogs(): string;

    /**
     * Reload configuration from D365 entity
     */
    reloadConfiguration(): Promise<boolean>;

    /**
     * Manually flush remote logs to D365
     */
    flushRemoteLogs(): Promise<void>;

    /** Static LogLevel enumeration */
    static LogLevel: typeof LogLevel;

    /** Static NotificationType enumeration */
    static NotificationType: typeof NotificationType;

    /** Static AlertIcon enumeration */
    static AlertIcon: typeof AlertIcon;
}

export default D365Client;
