namespace Platform.Customers.Domain;

public static class CustomersConstants
{
    public static class CustomerTypes
    {
        public const string ENTERPRISE = "ENTERPRISE";
        public const string SMALL_BUSINESS = "SMALL_BUSINESS";
        public const string RETAIL = "RETAIL";
        public const string INDIVIDUAL = "INDIVIDUAL";
    }
    
    public static class Exceptions
    {
        public static class CustomerNotFound
        {
            public const string TITLE = "Customer Not Found";
            public const string ERRORCODE = "CUSTOMER_NOT_FOUND";
        }

        public static class CustomerAlreadyExists
        {
            public const string TITLE = "Customer Already Exists";
            public const string ERRORCODE = "CUSTOMER_ALREADY_EXISTS";
        }

        public static class CustomerAlreadyActive
        {
            public const string TITLE = "Customer Already Active";
            public const string ERRORCODE = "CUSTOMER_ALREADY_ACTIVE";
        }

        public static class CustomerAlreadyInactive
        {
            public const string TITLE = "Customer Already Inactive";
            public const string ERRORCODE = "CUSTOMER_ALREADY_INACTIVE";
        }

        public static class EmailAlreadyExists
        {
            public const string TITLE = "Email Already Exists";
            public const string ERRORCODE = "EMAIL_ALREADY_EXISTS";
        }
    }
}