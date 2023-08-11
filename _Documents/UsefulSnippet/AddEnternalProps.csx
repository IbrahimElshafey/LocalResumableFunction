private void AddEnternalProps<T>(ModelBuilder modelBuilder)
{
    var nonPublicProps =
        typeof(T)
        .GetProperties(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
        .Where(propInfo =>
        propInfo.SetMethod != null && propInfo.GetMethod != null &&
        (propInfo.PropertyType.IsSimpleType() || propInfo.PropertyType == typeof(byte[])) &&
        propInfo.GetCustomAttribute<NotMappedAttribute>() == null
        );
    foreach (var prop in nonPublicProps)
    {
        modelBuilder.Entity(typeof(T)).Property(prop.PropertyType, prop.Name);
    }
}