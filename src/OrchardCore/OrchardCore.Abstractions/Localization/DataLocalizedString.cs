namespace OrchardCore.Localization
{
    public class DataLocalizedString
    {
        public DataLocalizedString(string context, string name)
        {
            Context = context;
            Name = name;
        }

        public string Context { get; }

        public string Name { get; }

        public override string ToString() => $"{Context}-{Name}";
    }
}
