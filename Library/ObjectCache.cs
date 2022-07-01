using System.Reflection;

namespace Net.Leksi.KeyBox
{
    public class ObjectCache
    {
        private readonly Dictionary<Type, Dictionary<object?[], object>> _cache = new();
        private readonly KeyEqualityComparer _keyEqualityComparer = new();

        public bool TryGet<T>(IKeyRing keyRing, out T? result)
        {
            object? tmp = null;
            bool success = TryGet(typeof(T), keyRing, out tmp);
            result = (T?)tmp;
            return success;
        }

        public bool TryGet(Type type, IKeyRing keyRing, out object? result)
        {
            if (keyRing is null)
            {
                throw new ArgumentNullException();
            }
            if (!keyRing.IsCompleted)
            {
                throw new ArgumentException($"{nameof(keyRing)} must be completed!");
            }
            result = null;
            object?[] selector = keyRing.Values.ToArray();
            if(!_cache.TryGetValue(type, out Dictionary<object?[], object>? dict) || !dict.TryGetValue(selector, out result))
            {
                return false;
            }
            Type actualType = result.GetType();
            if (type != actualType && (!type.IsInterface || !type.IsAssignableFrom(actualType)))
            {
                throw new ArgumentException($"{nameof(result)} must implement or be {type}!");
            }
            return true;
        }

        public object Add<T>(IKeyRing keyRing, T value)
        {
            return Add(typeof(T), keyRing, value);
        }

        public object Add(Type type, IKeyRing keyRing, object value)
        {
            if (keyRing is null)
            {
                throw new ArgumentNullException(nameof(keyRing));
            }
            if (value is null)
            {
                throw new ArgumentNullException(nameof(value));
            }
            if (!keyRing.IsCompleted)
            {
                throw new ArgumentException($"{nameof(keyRing)} must be completed!");
            }
            Type actualType = value.GetType();
            if (type != actualType && (!type.IsInterface || !type.IsAssignableFrom(actualType)))
            {
                throw new ArgumentException($"{nameof(value)} must implement or be {type}!");
            }

            if (!_cache.TryGetValue(actualType, out Dictionary<object?[], object>? actualDict))
            {
                actualDict = new Dictionary<object?[], object>(_keyEqualityComparer);
            }
            object?[] selector = keyRing.Values.ToArray();
            if (actualDict.TryGetValue(selector, out object? obj))
            {
                foreach(PropertyInfo pi in actualType.GetProperties())
                {
                    if(pi.CanWrite)
                    {
                        object? oldValue = pi.GetValue(obj);
                        object? newValue = pi.GetValue(value);
                        if(oldValue == default && newValue != default)
                        {
                            pi.SetValue(obj, newValue);
                        }
                    }
                }
                value = obj;
            }
            else
            {
                actualDict[selector] = value;
            }
            if(actualType != type)
            {
                if (!_cache.TryGetValue(type, out Dictionary<object?[], object>? dict))
                {
                    dict = new Dictionary<object?[], object>(_keyEqualityComparer);
                }
                dict[selector] = value;
            }
            return value;
        }
    }
}
