using Generic.LightDataTable.InterFace;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Generic.LightDataTable.Interface;
using Generic.LightDataTable.SqlQuerys;

namespace Generic.LightDataTable.Library
{

    internal sealed class SqlQueriable<T> : List<T>, ISqlQueriable<T> where T : class, IDbEntity
    {
        private readonly ICustomRepository _repository;
        private readonly List<string> _ignoreActions = new List<string>();
        private readonly LightDataLinqToNoSql<T> _expression = new LightDataLinqToNoSql<T>();
        private bool _executed = false;
        private readonly bool _partExecuted = false;
        private readonly List<Expression<Func<T, object>>> _childrenToLoad = new List<Expression<Func<T, object>>>();
        private bool? _loadcholdrenOnlyFirstLevel;
        private readonly List<Expression> _matches = new List<Expression>();

        internal SqlQueriable(List<T> items, ICustomRepository repository)
        {
            _repository = repository;
            if (items == null)
                return;
            _partExecuted = true;
            items.RemoveAll(x => x == null);
            base.AddRange(items);
        }

        public string ParsedLinqToSql { get; private set; }

        public new ISqlQueriable<T> Add(T item)
        {
            if (item != null)
                base.Add(item);
            return this;
        }

        public ISqlQueriable<T> AddRange(List<T> items)
        {
            if (items == null)
                return this;
            base.AddRange(items.Where(x => x != null));
            return this;
        }

        public ISqlQueriable<T> IgnoreChildren(params Expression<Func<T, object>>[] ignoreActions)
        {
            if (ignoreActions != null)
                _ignoreActions.AddRange(ignoreActions.ConvertExpressionToIncludeList(true));

            return this;
        }

        public ISqlQueriable<T> LoadChildren(bool onlyFirstLevel = false)
        {
            _loadcholdrenOnlyFirstLevel = onlyFirstLevel;
            return this;
        }

        public ISqlQueriable<T> LoadChildren(params Expression<Func<T, object>>[] actions)
        {
            if (actions != null)
                _childrenToLoad.AddRange(actions);
            return this;
        }

        public ISqlQueriable<T> Where(Expression<Predicate<T>> match)
        {
            _matches.Add(match);
            return this;
        }

        public ISqlQueriable<T> Take(int value)
        {
            _expression.Take = value;
            return this;
        }

        public ISqlQueriable<T> Skip(int value)
        {
            _expression.Skip = value;
            return this;
        }

        public ISqlQueriable<T> OrderBy(Expression<Func<T, object>> exp)
        {
            var list = Expression.Parameter(typeof(IEnumerable<T>), "list");
            var orderByExp = Expression.Call(typeof(Enumerable), "OrderBy", new Type[] { typeof(T), typeof(object) }, list, exp);
            _matches.Add(orderByExp);
            return this;
        }

        public ISqlQueriable<T> OrderByDescending(Expression<Func<T, object>> exp)
        {
            var list = Expression.Parameter(typeof(IEnumerable<T>), "list");
            var orderByExp = Expression.Call(typeof(Enumerable), "OrderByDescending", new Type[] { typeof(T), typeof(object) }, list, exp);
            _matches.Add(orderByExp);
            return this;
        }

        public List<T> Execute()
        {
            if (_executed)
                return this.ToList<T>();
            else
            {
                foreach (var exp in _matches)
                    _expression.Translate(exp);

                ParsedLinqToSql = _expression.Quary;
                if (!_partExecuted)
                    this.AddRange(!string.IsNullOrEmpty(_expression.Quary) ? _repository.Where<T>(_expression) : _repository.GetAbstractAll<T>());
                if (_childrenToLoad.Any() || _loadcholdrenOnlyFirstLevel.HasValue)
                {
                    foreach (var item in this)
                    {
                        if (_childrenToLoad.Any())
                            _repository.LoadChildren(item, false, _ignoreActions, _childrenToLoad.ToArray());
                        else _repository.LoadChildren(item, _loadcholdrenOnlyFirstLevel.Value, _ignoreActions);
                    }
                }
                _executed = true;
            }

            return this.ToList<T>();
        }

        public async Task<List<T>> ExecuteAsync()
        {
            if (_executed)
                return this.ToList<T>();
            else
            {
                foreach (var exp in _matches)
                    _expression.Translate(exp);

                ParsedLinqToSql = _expression.Quary;

                if (!_partExecuted)
                    this.AddRange(!string.IsNullOrEmpty(_expression.Quary) ? await _repository.WhereAsync<T>(_expression) : await _repository.GetAbstractAllAsync<T>());
                if (_childrenToLoad.Any() || _loadcholdrenOnlyFirstLevel.HasValue)
                {
                    foreach (var item in this)
                    {
                        if (_childrenToLoad.Any())
                            await _repository.LoadChildrenAsync(item, false, _ignoreActions, _childrenToLoad.ToArray());
                        else await _repository.LoadChildrenAsync(item, _loadcholdrenOnlyFirstLevel.Value, _ignoreActions);
                    }
                }
                _executed = true;
            }

            return this.ToList<T>();
        }

        public ISqlQueriable<T> Save()
        {
            GetRepository().CreateTransaction();
            foreach (var item in Execute())
                _repository.Save(item);
            return this;
        }

        public async Task<ISqlQueriable<T>> SaveAsync()
        {
            GetRepository().CreateTransaction();
            foreach (var item in Execute())
                await _repository.SaveAsync(item);
            return this;
        }

        public async Task<ISqlQueriable<T>> SaveAllAsync(Func<T, bool> match)
        {
            GetRepository().CreateTransaction();
            foreach (var item in Execute().Where(match))
                await _repository.SaveAsync(item);
            return this;
        }

        public ISqlQueriable<T> SaveAll(Func<T, bool> match)
        {
            GetRepository().CreateTransaction();
            foreach (var item in Execute().Where(match))
                _repository.Save(item);
            return this;
        }

        public async Task RemoveAsync()
        {
            GetRepository().CreateTransaction();
            foreach (var item in Execute())
                await _repository.DeleteAbstractAsync(item);
            this.Clear();
        }

        public void Remove()
        {
            GetRepository().CreateTransaction();
            foreach (var item in Execute())
                _repository.DeleteAbstract(item);
            this.Clear();
        }

        public async Task RemoveAllAsync(Func<T, bool> match)
        {
            GetRepository().CreateTransaction();
            foreach (var item in Execute().Where(match))
            {
                await _repository.DeleteAbstractAsync(item);
                base.Remove(item);
            }

            foreach (var item in Execute().Where(match))
                base.Remove(item);
        }

        public void RemoveAll(Func<T, bool> match)
        {
            GetRepository().CreateTransaction();
            foreach (var item in Execute().Where(match))
            {
                _repository.DeleteAbstract(item);
                base.Remove(item);
            }
            foreach (var item in Execute().Where(match))
                base.Remove(item);
        }

        public ICustomRepository GetRepository()
        {
            return _repository;
        }

        public ILightDataTable ToLightDataTable()
        {
            return new LightDataTable(Execute());
        }

        public List<TSource> ExecuteAndConvertToType<TSource>() where TSource : class
        {
            return Execute().ToType<List<TSource>>();
        }

        public void Dispose()
        {
            _repository?.Dispose();
        }
    }
}
