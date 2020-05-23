using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Policy;

namespace PascalCompiler.Syntax.Generator.Utils
{
    public class CommonUtils
    {
        public static Dictionary<object, HashSet<SyntaxPredicate>> CalculateFirstSet(List<ProductionRule> productionRules) {
            var result = new Dictionary<object, HashSet<SyntaxPredicate>>();
            foreach (var productionRule in productionRules) {
                if (!result.ContainsKey(productionRule.Key)) {
                    result.Add(productionRule.Key, new HashSet<SyntaxPredicate>());
                }
            }
            bool updated = true;
            while (updated) {
                updated = false;
                foreach (var productionRule in productionRules) {
                    var key = productionRule.Key;
                    if (productionRule.Length == 0) {
                        updated |= result[key].Add(EpsilonPredicate.Instance);
                        continue;
                    }

                    bool epl = true;
                    foreach (var predicate in productionRule.Predicates) {
                        if (predicate is TerminalPredicate) {
                            updated |= result[key].Add(predicate);
                            epl = false;
                            break;
                        }

                        if (predicate is KeyedNonTerminalPredicate kp) {
                            var t = kp.Key;
                            var tset = result[t].Except(EpsilonPredicate.Enumerator());
                            foreach (var syntaxPredicate in tset) {
                                updated |= result[key].Add(syntaxPredicate);
                            }
                            if (!result[t].Contains(EpsilonPredicate.Instance)) {
                                epl = false;
                                break;
                            }
                        }
                    }
                    if (epl) {
                        updated |= result[key].Add(EpsilonPredicate.Instance);
                        
                    }
                }
            }
            return result;
        }

        public static Dictionary<object, HashSet<SyntaxPredicate>> CalculateFollowSet(
            List<ProductionRule> productionRules, Dictionary<object, HashSet<SyntaxPredicate>> firstSet) {
            var result = new Dictionary<object, HashSet<SyntaxPredicate>>();
            foreach (var key in firstSet.Keys) {
                result.Add(key, new HashSet<SyntaxPredicate>());
            }
            bool updated = true;
            while (updated) {
                updated = false;
                foreach (var productionRule in productionRules) {
                    int i = 0;
                    for (; i < productionRule.Predicates.Count; ++i) {
                        if (productionRule.Predicates[i] is KeyedNonTerminalPredicate kp) {
                            bool epl = true;
                            for (int j = i + 1; j < productionRule.Predicates.Count; ++j) {
                                if (productionRule.Predicates[j] is TerminalPredicate tp) {
                                    epl = false;
                                    updated |= result[kp.Key].Add(tp);
                                    break;
                                }
                                if (productionRule.Predicates[j] is KeyedNonTerminalPredicate kp2) {
                                    var f = firstSet[kp2.Key];
                                    foreach (var source in f.Except(EpsilonPredicate.Enumerator())) {
                                        updated |= result[kp.Key].Add(source);
                                    }

                                    if (!f.Contains(EpsilonPredicate.Instance)) {
                                        epl = false;
                                        break;
                                    }
                                }
                            }

                            if (epl) {
                                foreach (var sp in result[productionRule.Key].Except(EpsilonPredicate.Enumerator())) {
                                    updated |= result[kp.Key].Add(sp);
                                }
                            }
                        }
                    }
                }
            }
            return result;
        }

        public static HashSet<Item> Closure(HashSet<Item> itemSet,
            MultiValueDictionary<object, ProductionRule> productionRules) {
            var result = new HashSet<Item>();
            var result2 = new HashSet<Item>();
            result.UnionWith(itemSet);
            bool updated = true;
            while (updated) {
                updated = false;
                result2.UnionWith(result);
                foreach (var item in result) {
                    if (item.Cursor < item.ProductionRule.Predicates.Count &&
                        item.ProductionRule.Predicates[item.Cursor] is KeyedNonTerminalPredicate np) {
                        foreach (var productionRule in productionRules[np.Key]) {
                            updated |= result2.Add(new Item() {ProductionRule = productionRule, Cursor = 0});
                        }
                    }
                }
                result.UnionWith(result2);
            }
            return result;
        }

        public static MultiValueDictionary<object, ProductionRule> MakeProductionRuleDictionary(
            IEnumerable<ProductionRule> productionRules) {
            var result = new MultiValueDictionary<object, ProductionRule>();
            foreach (var productionRule in productionRules) {
                result.Add(productionRule.Key, productionRule);
            }
            return result;
        }
    }

    public class MultiValueDictionary<KeyType, ValueType> : Dictionary<KeyType, List<ValueType>>
    {
        /// <summary>
        /// Hide the regular Dictionary Add method
        /// </summary>
        new private void Add(KeyType key, List<ValueType> value)
        {
            base.Add(key, value);
        }

        /// <summary>
        /// Adds the specified value to the multi value dictionary.
        /// </summary>
        /// <param name="key">The key of the element to add.</param>
        /// <param name="value">The value of the element to add. The value can be null for reference types.</param>
        public void Add(KeyType key, ValueType value)
        {
            //add the value to the dictionary under the key
            MultiValueDictionaryExtensions.Add(this, key, value);
        }
    }

    public static class MultiValueDictionaryExtensions
    {
        /// <summary>
        /// Adds the specified value to the multi value dictionary.
        /// </summary>
        /// <param name="key">The key of the element to add.</param>
        /// <param name="value">The value of the element to add. The value can be null for reference types.</param>
        public static void Add<KeyType, ListType, ValueType>(this Dictionary<KeyType, ListType> thisDictionary,
            KeyType key, ValueType value)
            where ListType : IList<ValueType>, new()
        {
            //if the dictionary doesn't contain the key, make a new list under the key
            if (!thisDictionary.ContainsKey(key))
            {
                thisDictionary.Add(key, new ListType());
            }

            //add the value to the list at the key index
            thisDictionary[key].Add(value);
        }
    }
}
