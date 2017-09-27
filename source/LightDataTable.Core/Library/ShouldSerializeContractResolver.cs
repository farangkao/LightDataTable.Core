using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Generic.LightDataTable.Attributes;
using Generic.LightDataTable.InterFace;

namespace Generic.LightDataTable.Library
{
    public sealed class ShouldSerializeContractResolver : DefaultContractResolver
    {
        public new static readonly ShouldSerializeContractResolver Instance = new ShouldSerializeContractResolver();
        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            var property = base.CreateProperty(member, memberSerialization);
            property.ShouldSerialize = instance =>
                {
                    var e = (IDbEntity)instance;
                    if (e == null)
                        return true;
                    var prop = FastDeepCloner.DeepCloner.GetFastDeepClonerProperties(instance.GetType()).Find(x=> x.Name == property.PropertyName);
                    if (prop.ContainAttribute<PrimaryKey>())
                        return true;
                    return e.PropertyChanges.ContainsKey(property.PropertyName);
                };
            return property;
        }
    }
}
