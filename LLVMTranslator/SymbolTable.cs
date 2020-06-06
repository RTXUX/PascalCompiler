using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LLVMSharp;
using LLVMSharp.Interop;

namespace LLVMTranslator
{
    public class LLVMSymbolTable {
        private readonly LLVMSymbolTable _parent;
        private readonly Dictionary<string, LLVMValueRef> _symbols;

        public LLVMSymbolTable(LLVMSymbolTable parent) {
            _parent = parent;
            _symbols = new Dictionary<string, LLVMValueRef>();
        }

        public LLVMValueRef Lookup(string key) {
            if (_symbols.TryGetValue(key, out var res)) {
                return res;
            } else {
                return _parent?.Lookup(key) ?? throw new KeyNotFoundException(key);
            }
        }

        public void Add(string key, LLVMValueRef value) {
            _symbols.Add(key, value);
        }

        public bool Contains(string key) {
            return _symbols.ContainsKey(key);
        }

        public LLVMValueRef this[string key] => Lookup(key);
    }
}
