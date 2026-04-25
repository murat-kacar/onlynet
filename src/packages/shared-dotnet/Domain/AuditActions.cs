namespace TabFlow.Shared.Domain;

public static class AuditActions
{
    public static class Auth
    {
        public const string LoginSuccess = "auth.login.success";
        public const string LoginFailure = "auth.login.failure";
        public const string PasswordChange = "auth.password.change";
        public const string Logout = "auth.logout";
    }

    public static class User
    {
        public const string Create = "user.create";
        public const string Update = "user.update";
        public const string RoleChange = "user.role.change";
        public const string Deactivate = "user.deactivate";
    }

    public static class Tenant
    {
        public const string Create = "tenant.create";
        public const string StatusChange = "tenant.status.change";
        public const string RegionalUpdate = "tenant.regional.update";
    }

    public static class Bill
    {
        public const string Close = "bill.close";
        public const string Move = "bill.move";
        public const string Merge = "bill.merge";
        public const string Split = "bill.split";
    }

    public static class Catalog
    {
        public const string CategoryCreate = "catalog.category.create";
        public const string CategoryUpdate = "catalog.category.update";
        public const string CategoryDelete = "catalog.category.delete";
        public const string ItemCreate = "catalog.item.create";
        public const string ItemUpdate = "catalog.item.update";
        public const string ItemDelete = "catalog.item.delete";
    }

    public static class Station
    {
        public const string Create = "station.create";
        public const string Update = "station.update";
        public const string Delete = "station.delete";
        public const string DevicePair = "station.device.pair";
        public const string DeviceRevoke = "station.device.revoke";
    }

    public static class Provision
    {
        public const string JobTrigger = "provision.job.trigger";
    }
}
