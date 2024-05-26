using System.Reflection;
using System.Text;

public class IniFile
{
    private readonly string _path;
    private readonly Dictionary<string, Dictionary<string, string>> _data;

    public IniFile(string iniPath)
    {
        _path = iniPath;
        _data = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

        if (File.Exists(_path))
        {
            Load(_path);
        }
    }

    private void Load(string path)
    {
        string currentSection = null;

        using (var reader = new StreamReader(path, Encoding.UTF8))
        {
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                var trimmedLine = line.Trim();

                // Skip empty lines and comments
                if (string.IsNullOrWhiteSpace(trimmedLine) || trimmedLine.StartsWith(";"))
                    continue;

                // Handle section headers
                if (trimmedLine.StartsWith("[") && trimmedLine.EndsWith("]"))
                {
                    currentSection = trimmedLine[1..^1];
                    if (!_data.ContainsKey(currentSection))
                    {
                        _data[currentSection] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    }
                }
                else
                {
                    // Handle key-value pairs
                    var keyValue = trimmedLine.Split(['='], 2);
                    if (keyValue.Length == 2)
                    {
                        var key = keyValue[0].Trim();
                        var value = keyValue[1].Trim();

                        if (currentSection == null)
                        {
                            currentSection = "Default";
                            if (!_data.ContainsKey(currentSection))
                            {
                                _data[currentSection] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                            }
                        }

                        _data[currentSection][key] = value;
                    }
                }
            }
        }
    }

    public void Write(string section, string key, string value)
    {
        if (!_data.ContainsKey(section))
        {
            _data[section] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
        _data[section][key] = value;
        Save();
    }

    public string Read(string section, string key, string defaultValue = "")
    {
        if (_data.ContainsKey(section) && _data[section].ContainsKey(key))
        {
            return _data[section][key];
        }
        return defaultValue;
    }

    private void Save()
    {
        using (var writer = new StreamWriter(_path, false, Encoding.UTF8))
        {
            foreach (var section in _data)
            {
                writer.WriteLine($"[{section.Key}]");
                foreach (var kvp in section.Value)
                {
                    writer.WriteLine($"{kvp.Key}={kvp.Value}");
                }
                writer.WriteLine();
            }
        }
    }

    public void WriteObject<T>(string section, T obj)
    {
        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
        foreach (var property in properties)
        {
            var value = property.GetValue(obj)?.ToString() ?? string.Empty;
            Write(section, property.Name, value);
        }
    }

    public T ReadObject<T>(string section) where T : new()
    {
        var obj = new T();
        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
        foreach (var property in properties)
        {
            var value = Read(section, property.Name);
            if (!string.IsNullOrEmpty(value))
            {
                if (property.PropertyType == typeof(string))
                {
                    property.SetValue(obj, value);
                }
                // Add more type checks if needed for other property types
            }
        }
        return obj;
    }
}