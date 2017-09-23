using System;
using System.Linq;

namespace Generic.LightDataTable.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class Rule : Attribute
    {
        public Type RuleType { get; private set; }

        /// <inheritdoc />
        /// <summary>
        /// Define class rule by adding this attribute
        /// ruleType must inhert from IDbRuleTrigger
        /// </summary>
        /// <param name="ruleType"></param>
        public Rule(Type ruleType)
        {
            RuleType = ruleType;
            if (ruleType.GetInterfaces().Length <= 0 || !ruleType.GetInterfaces().Any(x => x.ToString().Contains("IDbRuleTrigger")))
                throw new Exception("RuleType dose not implement inteface IDbRuleTrigger");
        }
    }
}
