namespace CMA.Utilities.MembershipReport;

/// <summary>
/// Describes the Dataverse query and each report's filters/grouping/fields. Injected into the
/// report (for the in-page "Download schema" button) and written to a sibling <c>.schema.json</c>.
/// Kept in step with the fallback schema in report-template.html.
/// </summary>
public static class ReportSchema
{
    public static object Build(string environment, string generatedOn) => new
    {
        generatedOn,
        environment,
        entity = "new_cmamembershipdetail",
        query = new
        {
            description = "Single paged RetrieveMultiple (NoLock) ordered by createdon desc, with LeftOuter joins to Contact, Product and Account.",
            columns = new[]
            {
                "new_cmamembershipdetailid", "new_contact", "new_categoryproductid",
                "new_divassocaccountid", "new_expirydate", "new_membershipyear",
                "new_status", "createdon"
            },
            joins = new object[]
            {
                new { entity = "contact", alias = "con",  from = "contactid",  to = "new_contact",           linkType = "outer", columns = new[] { "fullname", "gendercode", "new_age", "new_language" } },
                new { entity = "product", alias = "prod", from = "productid",  to = "new_categoryproductid", linkType = "outer", columns = new[] { "name" } },
                new { entity = "account", alias = "acct", from = "accountid",  to = "new_divassocaccountid", linkType = "outer", columns = new[] { "name" } }
            },
            fetchXml =
                "<fetch no-lock=\"true\"><entity name=\"new_cmamembershipdetail\">" +
                "<attribute name=\"new_cmamembershipdetailid\"/><attribute name=\"new_contact\"/>" +
                "<attribute name=\"new_categoryproductid\"/><attribute name=\"new_divassocaccountid\"/>" +
                "<attribute name=\"new_expirydate\"/><attribute name=\"new_membershipyear\"/>" +
                "<attribute name=\"new_status\"/><attribute name=\"createdon\"/>" +
                "<order attribute=\"createdon\" descending=\"true\"/>" +
                "<link-entity name=\"contact\" from=\"contactid\" to=\"new_contact\" link-type=\"outer\" alias=\"con\">" +
                "<attribute name=\"fullname\"/><attribute name=\"gendercode\"/><attribute name=\"new_age\"/><attribute name=\"new_language\"/></link-entity>" +
                "<link-entity name=\"product\" from=\"productid\" to=\"new_categoryproductid\" link-type=\"outer\" alias=\"prod\"><attribute name=\"name\"/></link-entity>" +
                "<link-entity name=\"account\" from=\"accountid\" to=\"new_divassocaccountid\" link-type=\"outer\" alias=\"acct\"><attribute name=\"name\"/></link-entity>" +
                "</entity></fetch>"
        },
        rules = new
        {
            season = "A membership season for year Y runs Sept (Y-1) → Dec (Y); createdon inside that window is in-season and charted by month. A year whose records all drift 2+ years (e.g. migrated) is shown as a single year total; the latest year always uses the month view.",
            status = "Renewals set a member's prior-year memberships inactive, so the Active filter applies only to the current calendar year; memberships for any other year are shown regardless of status."
        },
        reports = new
        {
            membership = new
            {
                title = "Membership",
                description = "Memberships for the selected year, by month created and by PTMA, with a detail table.",
                filters = new[] { "new_membershipyear (All years / a year / blank)", "new_status (Active applies to current year only, or All)", "new_divassocaccountid (PTMA)", "new_categoryproductid (Category)" },
                grouping = new[] { "createdon month (in-season only)", "new_divassocaccountid → account name" },
                fields = new[] { "contact.fullname", "product.name", "account.name", "new_expirydate", "new_membershipyear", "new_status", "createdon" }
            },
            overview = new
            {
                title = "Overview",
                description = "Active membership KPIs, years-to-expiry distribution, and members-by-type donut.",
                filters = new[] { "new_membershipyear", "new_status (current-year active rule)" },
                grouping = new[] { "member type derived from product.name (Physician/Student/Resident/Lifetime)", "whole years between today and new_expirydate" },
                fields = new[] { "product.name", "new_expirydate", "new_membershipyear", "new_status" }
            },
            demographics = new
            {
                title = "Demographics",
                description = "Distinct contacts by age range, gender and language.",
                filters = new[] { "new_status (Active or All)", "de-duplicated by new_contact (latest membership per contact)" },
                grouping = new[] { "contact.new_age → age band", "contact.gendercode", "contact.new_language" },
                fields = new[] { "contact.new_age", "contact.gendercode", "contact.new_language", "new_contact", "new_status" }
            },
            expiry = new
            {
                title = "Upcoming Expiry",
                description = "Active memberships about to expire, by month, within a chosen window.",
                filters = new[] { "new_status (Active or All)", "new_expirydate within N days of today" },
                grouping = new[] { "new_expirydate by month (next 12 months)" },
                fields = new[] { "contact.fullname", "product.name", "account.name", "new_expirydate", "new_status" }
            },
            contact = new
            {
                title = "Contact 360",
                description = "One contact and every membership year on file.",
                filters = new[] { "search by contact.fullname", "grouped by new_contact" },
                grouping = new[] { "all new_cmamembershipdetail rows per new_contact, by new_membershipyear" },
                fields = new[] { "contact.fullname", "contact.gendercode", "contact.new_age", "contact.new_language", "new_membershipyear", "new_status", "new_categoryproductid → name", "new_divassocaccountid → name", "new_expirydate", "createdon" }
            }
        }
    };
}
