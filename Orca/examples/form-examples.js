/**
 * D365Client - Real-World Usage Examples
 * Common scenarios and patterns for Dynamics 365 development
 */

// ============================================================================
// EXAMPLE 1: Complete Form Script with Validation
// ============================================================================

var CompanyNamespace = CompanyNamespace || {};

CompanyNamespace.ContactForm = {
    d365: null,

    /**
     * Form OnLoad event handler
     */
    onLoad: function(executionContext) {
        // Initialize D365Client with INFO logging
        this.d365 = new D365Client({
            logLevel: D365Client.LogLevel.INFO,
            autoReloadOnLogLevelChange: false
        });

        const formContext = executionContext.getFormContext();
        const formType = formContext.ui.getFormType();

        this.d365.info("ContactForm", "Form loaded", { formType: formType });

        // Form type: 1=Create, 2=Update, 3=Read Only, 4=Disabled, 6=Bulk Edit
        if (formType === 1) {
            this.handleNewContact(formContext);
        } else if (formType === 2) {
            this.handleExistingContact(formContext);
        }

        // Set up field change handlers
        this.registerFieldHandlers(formContext);
    },

    /**
     * Handle new contact form
     */
    handleNewContact: function(formContext) {
        this.d365.info("ContactForm", "Setting up new contact form");

        // Set default values
        formContext.getAttribute("preferredcontactmethodcode").setValue(1); // Email
        
        // Show notification
        this.d365.notification.setFormNotification(
            formContext,
            "Complete all required fields to save this contact",
            "INFO",
            "new_contact_info"
        );
    },

    /**
     * Handle existing contact form
     */
    handleExistingContact: function(formContext) {
        const contactId = formContext.data.entity.getId().replace(/[{}]/g, "");
        this.d365.info("ContactForm", "Loading existing contact", { contactId: contactId });

        // Load related data
        this.d365.utility.showProgressIndicator("Loading related data...");
        
        Promise.all([
            this.loadRelatedActivities(contactId),
            this.loadRelatedOpportunities(contactId)
        ])
        .then(([activities, opportunities]) => {
            this.d365.info("ContactForm", "Related data loaded", {
                activities: activities.length,
                opportunities: opportunities.length
            });
            
            // Update form based on data
            if (opportunities.length > 5) {
                this.d365.notification.setFormNotification(
                    formContext,
                    `This contact has ${opportunities.length} active opportunities`,
                    "INFO",
                    "opportunity_count"
                );
            }
        })
        .catch(error => {
            this.d365.error("ContactForm", "Failed to load related data", error);
            this.d365.notification.showError("Failed to load some related data", error);
        })
        .finally(() => {
            this.d365.utility.closeProgressIndicator();
        });
    },

    /**
     * Load related activities
     */
    loadRelatedActivities: async function(contactId) {
        const fetchXml = `
            <fetch top="50">
                <entity name="activitypointer">
                    <attribute name="subject" />
                    <attribute name="scheduledstart" />
                    <attribute name="activitytypecode" />
                    <filter>
                        <condition attribute="regardingobjectid" operator="eq" value="${contactId}" />
                        <condition attribute="statecode" operator="eq" value="0" />
                    </filter>
                    <order attribute="scheduledstart" descending="true" />
                </entity>
            </fetch>
        `;

        const options = `?fetchXml=${encodeURIComponent(fetchXml)}`;
        const result = await this.d365.webApi.retrieveMultipleRecords("activitypointer", options);
        return result.entities;
    },

    /**
     * Load related opportunities
     */
    loadRelatedOpportunities: async function(contactId) {
        const options = `?$select=name,estimatedvalue,estimatedclosedate&$filter=_parentcontactid_value eq ${contactId} and statecode eq 0&$orderby=estimatedclosedate asc`;
        const result = await this.d365.webApi.retrieveMultipleRecords("opportunity", options);
        return result.entities;
    },

    /**
     * Register field change handlers
     */
    registerFieldHandlers: function(formContext) {
        // Email validation
        formContext.getAttribute("emailaddress1").addOnChange(this.validateEmail.bind(this));
        
        // Phone number formatting
        formContext.getAttribute("telephone1").addOnChange(this.formatPhoneNumber.bind(this));
        
        // Account lookup change
        formContext.getAttribute("parentcustomerid").addOnChange(this.onAccountChange.bind(this));
    },

    /**
     * Validate email address
     */
    validateEmail: function(executionContext) {
        const formContext = executionContext.getFormContext();
        const emailAttribute = formContext.getAttribute("emailaddress1");
        const emailValue = emailAttribute.getValue();

        if (emailValue && !this.isValidEmail(emailValue)) {
            this.d365.notification.setAttributeNotification(
                emailAttribute,
                "Please enter a valid email address (e.g., name@company.com)",
                "email_validation"
            );
            this.d365.warn("ContactForm", "Invalid email format", { email: emailValue });
        } else {
            this.d365.notification.clearAttributeNotification(emailAttribute, "email_validation");
        }
    },

    /**
     * Format phone number
     */
    formatPhoneNumber: function(executionContext) {
        const formContext = executionContext.getFormContext();
        const phoneAttribute = formContext.getAttribute("telephone1");
        const phoneValue = phoneAttribute.getValue();

        if (phoneValue) {
            // Remove all non-numeric characters
            const cleaned = phoneValue.replace(/\D/g, "");
            
            // Format as (XXX) XXX-XXXX
            if (cleaned.length === 10) {
                const formatted = `(${cleaned.substr(0, 3)}) ${cleaned.substr(3, 3)}-${cleaned.substr(6, 4)}`;
                phoneAttribute.setValue(formatted);
                this.d365.debug("ContactForm", "Phone number formatted", { original: phoneValue, formatted: formatted });
            }
        }
    },

    /**
     * Handle account lookup change
     */
    onAccountChange: async function(executionContext) {
        const formContext = executionContext.getFormContext();
        const accountLookup = formContext.getAttribute("parentcustomerid").getValue();

        if (accountLookup && accountLookup.length > 0) {
            const accountId = accountLookup[0].id.replace(/[{}]/g, "");
            
            this.d365.info("ContactForm", "Account changed", { accountId: accountId });
            
            try {
                // Retrieve account information
                const account = await this.d365.webApi.retrieveRecord(
                    "account",
                    accountId,
                    "?$select=address1_city,address1_stateorprovince,address1_postalcode,telephone1"
                );

                // Suggest copying address information
                if (account.address1_city || account.address1_stateorprovince) {
                    const confirmed = await this.d365.notification.showConfirm(
                        "Would you like to copy the account's address to this contact?",
                        "Copy Address"
                    );

                    if (confirmed) {
                        formContext.getAttribute("address1_city").setValue(account.address1_city);
                        formContext.getAttribute("address1_stateorprovince").setValue(account.address1_stateorprovince);
                        formContext.getAttribute("address1_postalcode").setValue(account.address1_postalcode);
                        
                        this.d365.info("ContactForm", "Address copied from account");
                    }
                }
            } catch (error) {
                this.d365.error("ContactForm", "Failed to retrieve account", error);
            }
        }
    },

    /**
     * Form OnSave event handler
     */
    onSave: function(executionContext) {
        const formContext = executionContext.getFormContext();
        const eventArgs = executionContext.getEventArgs();

        this.d365.info("ContactForm", "Form save initiated");

        // Validate required fields
        const emailValue = formContext.getAttribute("emailaddress1").getValue();
        const phoneValue = formContext.getAttribute("telephone1").getValue();

        if (!emailValue && !phoneValue) {
            eventArgs.preventDefault();
            
            this.d365.notification.setFormNotification(
                formContext,
                "Please provide at least an email address or phone number",
                "ERROR",
                "contact_method_required"
            );
            
            this.d365.warn("ContactForm", "Save prevented: No contact method provided");
            return;
        }

        // Clear validation notifications on successful save
        this.d365.notification.clearFormNotification(formContext, "contact_method_required");
        this.d365.info("ContactForm", "Form save validation passed");
    },

    /**
     * Email validation helper
     */
    isValidEmail: function(email) {
        const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
        return emailRegex.test(email);
    }
};

// ============================================================================
// EXAMPLE 2: Ribbon Button - Bulk Update
// ============================================================================

CompanyNamespace.BulkOperations = {
    /**
     * Bulk activate accounts
     */
    bulkActivateAccounts: async function() {
        const d365 = new D365Client({ logLevel: D365Client.LogLevel.INFO });

        try {
            // Get selected records from grid
            const selectedRecords = this.getSelectedRecords();

            if (selectedRecords.length === 0) {
                await d365.notification.showAlert("Please select at least one account to activate");
                return;
            }

            // Confirm action
            const confirmed = await d365.notification.showConfirm(
                `Are you sure you want to activate ${selectedRecords.length} account(s)?`,
                "Confirm Bulk Activation"
            );

            if (!confirmed) {
                d365.info("BulkOperations", "Bulk activation cancelled by user");
                return;
            }

            d365.utility.showProgressIndicator(`Activating ${selectedRecords.length} accounts...`);

            // Update records in batches
            const results = await this.processBatch(d365, selectedRecords, async (recordId) => {
                return await d365.webApi.updateRecord("account", recordId, {
                    statecode: 0,
                    statuscode: 1
                });
            });

            d365.utility.closeProgressIndicator();

            // Show results
            const successCount = results.filter(r => r.success).length;
            const failCount = results.filter(r => !r.success).length;

            await d365.notification.showAlert(
                `Activation complete: ${successCount} succeeded, ${failCount} failed`,
                "Bulk Activation Results"
            );

            // Refresh grid
            d365.utility.refreshParentGrid();

        } catch (error) {
            d365.error("BulkOperations", "Bulk activation failed", error);
            d365.utility.closeProgressIndicator();
            await d365.notification.showError("Bulk activation failed", error);
        }
    },

    /**
     * Process records in batch
     */
    processBatch: async function(d365, recordIds, processFunc) {
        const batchSize = 10;
        const results = [];

        for (let i = 0; i < recordIds.length; i += batchSize) {
            const batch = recordIds.slice(i, i + batchSize);
            d365.debug("BulkOperations", `Processing batch ${Math.floor(i / batchSize) + 1}`, { size: batch.length });

            const batchPromises = batch.map(async (recordId) => {
                try {
                    await processFunc(recordId);
                    return { recordId, success: true };
                } catch (error) {
                    d365.error("BulkOperations", `Failed to process record ${recordId}`, error);
                    return { recordId, success: false, error };
                }
            });

            const batchResults = await Promise.all(batchPromises);
            results.push(...batchResults);
        }

        return results;
    },

    /**
     * Get selected records from grid
     */
    getSelectedRecords: function() {
        const gridControl = Xrm.Page.getControl("Accounts");
        if (!gridControl) return [];

        const grid = gridControl.getGrid();
        const selectedRows = grid.getSelectedRows();
        const recordIds = [];

        selectedRows.forEach(row => {
            recordIds.push(row.getId().replace(/[{}]/g, ""));
        });

        return recordIds;
    }
};

// ============================================================================
// EXAMPLE 3: Custom Action Execution
// ============================================================================

CompanyNamespace.CustomActions = {
    /**
     * Execute custom action to calculate opportunity score
     */
    calculateOpportunityScore: async function(formContext) {
        const d365 = new D365Client({ logLevel: D365Client.LogLevel.DEBUG });

        const opportunityId = formContext.data.entity.getId().replace(/[{}]/g, "");
        d365.info("CustomActions", "Calculating opportunity score", { opportunityId });

        d365.utility.showProgressIndicator("Calculating opportunity score...");

        try {
            // Prepare action parameters
            const parameters = {
                Target: {
                    "@odata.type": "Microsoft.Dynamics.CRM.opportunity",
                    opportunityid: opportunityId
                }
            };

            // Execute custom action
            const result = await d365.utility.invokeProcessAction("new_CalculateOpportunityScore", parameters);

            d365.utility.closeProgressIndicator();

            // Update form with result
            if (result && result.Score) {
                formContext.getAttribute("new_score").setValue(result.Score);
                formContext.data.entity.save();

                await d365.notification.showAlert(
                    `Opportunity score calculated: ${result.Score}`,
                    "Score Calculation Complete"
                );

                d365.info("CustomActions", "Score calculated successfully", { score: result.Score });
            }

        } catch (error) {
            d365.utility.closeProgressIndicator();
            d365.error("CustomActions", "Failed to calculate score", error);
            await d365.notification.showError("Failed to calculate opportunity score", error);
        }
    },

    /**
     * Execute workflow
     */
    executeWorkflow: async function(entityName, recordId, workflowId) {
        const d365 = new D365Client();

        d365.info("CustomActions", "Executing workflow", { entityName, recordId, workflowId });

        try {
            // Build request
            const request = {
                EntityId: {
                    guid: recordId
                },
                WorkflowId: {
                    guid: workflowId
                },

                getMetadata: function() {
                    return {
                        boundParameter: null,
                        parameterTypes: {
                            "EntityId": {
                                "typeName": "mscrm.crmbaseentity",
                                "structuralProperty": 5
                            },
                            "WorkflowId": {
                                "typeName": "Edm.Guid",
                                "structuralProperty": 1
                            }
                        },
                        operationType: 0,
                        operationName: "ExecuteWorkflow"
                    };
                }
            };

            const result = await d365.webApi.execute(request);
            d365.info("CustomActions", "Workflow executed successfully");
            return result;

        } catch (error) {
            d365.error("CustomActions", "Workflow execution failed", error);
            throw error;
        }
    }
};

// ============================================================================
// EXAMPLE 4: Advanced Query with Error Handling
// ============================================================================

CompanyNamespace.DataRetrieval = {
    /**
     * Get active opportunities with related data
     */
    getActiveOpportunities: async function() {
        const d365 = new D365Client({ logLevel: D365Client.LogLevel.DEBUG });

        try {
            const options = `?$select=name,estimatedvalue,estimatedclosedate
                &$expand=customerid_account($select=name,revenue)
                &$filter=statecode eq 0 and estimatedvalue gt 10000
                &$orderby=estimatedclosedate asc
                &$top=50`;

            d365.debug("DataRetrieval", "Fetching active opportunities");

            const result = await d365.webApi.retrieveMultipleRecords("opportunity", options);

            d365.info("DataRetrieval", `Retrieved ${result.entities.length} opportunities`);

            // Process and return data
            return result.entities.map(opp => ({
                id: opp.opportunityid,
                name: opp.name,
                value: opp.estimatedvalue,
                closeDate: opp.estimatedclosedate,
                accountName: opp.customerid_account?.name,
                accountRevenue: opp.customerid_account?.revenue
            }));

        } catch (error) {
            d365.error("DataRetrieval", "Failed to retrieve opportunities", error);
            throw error;
        }
    },

    /**
     * Get all pages of results
     */
    getAllRecords: async function(entityName, baseOptions) {
        const d365 = new D365Client();
        let allRecords = [];
        let nextLink = null;

        try {
            // Get first page
            let result = await d365.webApi.retrieveMultipleRecords(entityName, baseOptions);
            allRecords = allRecords.concat(result.entities);
            nextLink = result.nextLink;

            d365.debug("DataRetrieval", `Retrieved page 1: ${result.entities.length} records`);

            // Get remaining pages
            let pageCount = 1;
            while (nextLink) {
                pageCount++;
                
                // Extract options from nextLink
                const url = new URL(nextLink);
                const pagingCookie = url.searchParams.get("$skiptoken");
                const nextOptions = baseOptions + `&$skiptoken=${pagingCookie}`;

                result = await d365.webApi.retrieveMultipleRecords(entityName, nextOptions);
                allRecords = allRecords.concat(result.entities);
                nextLink = result.nextLink;

                d365.debug("DataRetrieval", `Retrieved page ${pageCount}: ${result.entities.length} records`);
            }

            d365.info("DataRetrieval", `Retrieved all records: ${allRecords.length} total across ${pageCount} pages`);
            return allRecords;

        } catch (error) {
            d365.error("DataRetrieval", "Failed to retrieve all records", error);
            throw error;
        }
    }
};

// ============================================================================
// EXAMPLE 5: Complex Validation and Business Logic
// ============================================================================

CompanyNamespace.BusinessLogic = {
    d365: null,

    /**
     * Initialize business logic
     */
    initialize: function() {
        this.d365 = new D365Client({
            logLevel: D365Client.LogLevel.INFO
        });
    },

    /**
     * Validate opportunity before closing
     */
    validateOpportunityClose: async function(executionContext) {
        if (!this.d365) this.initialize();

        const formContext = executionContext.getFormContext();
        const eventArgs = executionContext.getEventArgs();
        
        // Get current values
        const stateCode = formContext.getAttribute("statecode").getValue();
        const revenue = formContext.getAttribute("actualvalue").getValue();
        const closeDate = formContext.getAttribute("actualclosedate").getValue();

        // Check if opportunity is being closed
        if (stateCode === 1) { // Won or Lost
            this.d365.info("BusinessLogic", "Validating opportunity closure");

            // Validation: Revenue required if won
            if (!revenue || revenue <= 0) {
                eventArgs.preventDefault();
                
                this.d365.notification.setFormNotification(
                    formContext,
                    "Actual revenue is required when closing an opportunity as won",
                    "ERROR",
                    "revenue_required"
                );
                
                this.d365.warn("BusinessLogic", "Validation failed: Revenue required");
                return;
            }

            // Validation: Close date required
            if (!closeDate) {
                eventArgs.preventDefault();
                
                this.d365.notification.setFormNotification(
                    formContext,
                    "Actual close date is required when closing an opportunity",
                    "ERROR",
                    "closedate_required"
                );
                
                this.d365.warn("BusinessLogic", "Validation failed: Close date required");
                return;
            }

            // Check for required notes
            const hasNotes = await this.checkForNotes(formContext);
            if (!hasNotes) {
                const confirmed = await this.d365.notification.showConfirm(
                    "No notes found for this opportunity. Proceed anyway?",
                    "Missing Notes"
                );

                if (!confirmed) {
                    eventArgs.preventDefault();
                    return;
                }
            }

            this.d365.info("BusinessLogic", "Opportunity closure validation passed");
        }
    },

    /**
     * Check if opportunity has notes
     */
    checkForNotes: async function(formContext) {
        const opportunityId = formContext.data.entity.getId().replace(/[{}]/g, "");
        
        const options = `?$select=annotationid&$filter=_objectid_value eq ${opportunityId}&$top=1`;
        
        try {
            const result = await this.d365.webApi.retrieveMultipleRecords("annotation", options);
            return result.entities.length > 0;
        } catch (error) {
            this.d365.error("BusinessLogic", "Failed to check for notes", error);
            return true; // Assume notes exist on error
        }
    }
};

// ============================================================================
// EXAMPLE 6: Global Error Handler
// ============================================================================

CompanyNamespace.ErrorHandler = {
    d365: new D365Client({ logLevel: D365Client.LogLevel.ERROR }),

    /**
     * Initialize global error handler
     */
    initialize: function() {
        window.onerror = this.handleError.bind(this);
        window.addEventListener('unhandledrejection', this.handlePromiseRejection.bind(this));
    },

    /**
     * Handle JavaScript errors
     */
    handleError: function(message, source, lineno, colno, error) {
        this.d365.error("GlobalErrorHandler", "JavaScript error occurred", {
            message: message,
            source: source,
            line: lineno,
            column: colno,
            error: error
        });

        // Show user-friendly error
        this.d365.notification.showError(
            "An unexpected error occurred",
            "Please try again or contact your system administrator"
        );

        return true; // Prevent default error handling
    },

    /**
     * Handle promise rejections
     */
    handlePromiseRejection: function(event) {
        this.d365.error("GlobalErrorHandler", "Unhandled promise rejection", {
            reason: event.reason,
            promise: event.promise
        });

        event.preventDefault();
    }
};

// Initialize global error handler
CompanyNamespace.ErrorHandler.initialize();
