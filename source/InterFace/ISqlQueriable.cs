using Generic.LightDataTable.Interface;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Generic.LightDataTable.InterFace
{
    /// <summary>
    /// LightDataTable Provider
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface ISqlQueriable<T> where T : class, IDbEntity
    {
        /// <summary>
        /// Result of LightDataTable LinqToSql
        /// </summary>
        string ParsedLinqToSql { get; }

        /// <summary>
        /// Load All children/ sub object
        /// this will do left join
        /// </summary>
        /// <param name="onlyFirstLevel">true for level 1 eg User.Role</param>
        /// <returns></returns>
        ISqlQueriable<T> LoadChildren(bool onlyFirstLevel = false);
        /// <summary>
        /// load specified children by Expression
        /// will load all selected object childrens levels if IgnoreChildren not specified
        /// </summary>
        /// <param name="actions"></param>
        /// <returns></returns>
        ISqlQueriable<T> LoadChildren(params Expression<Func<T, object>>[] actions);
        /// <summary>
        /// Ignore loading properties
        /// </summary>
        /// <param name="ignoreActions"></param>
        /// <returns></returns>
        ISqlQueriable<T> IgnoreChildren(params Expression<Func<T, object>>[] ignoreActions);
        /// <summary>
        /// Execute the quary and return generict List of T
        /// </summary>
        /// <returns></returns>
        List<T> Execute();
        /// <summary>
        /// Execute the quary and return generict List or T
        /// </summary>
        /// <returns></returns>
        Task<List<T>> ExecuteAsync();
        /// <summary>
        /// Get the repository
        /// </summary>
        /// <returns></returns>
        ICustomRepository GetRepository();
        /// <summary>
        /// Convert the current List to LightDataTable
        /// </summary>
        /// <returns></returns>
        ILightDataTable ToLightDataTable();
        /// <summary>
        /// Add Items
        /// </summary>
        /// <param name="items"></param>
        /// <returns></returns>
        ISqlQueriable<T> AddRange(List<T> items);
        /// <summary>
        /// Add item
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        ISqlQueriable<T> Add(T item);
        /// <summary>
        /// Build sql Where by Linq
        /// </summary>
        /// <param name="match"></param>
        /// <returns></returns>
        ISqlQueriable<T> Where(Expression<Predicate<T>> match);
        /// <summary>
        /// Save object Herarkie, will Update or Insert all object that containe IndependedData attributes
        /// </summary>
        /// <returns></returns>
        ISqlQueriable<T> Save();
        /// <summary>
        /// Save object Herarkie, will Update or Insert all object that containe IndependedData attributes
        /// </summary>
        /// <returns></returns>
        Task<ISqlQueriable<T>> SaveAsync();
        /// <summary>
        /// Save object Herarkie, will Update or Insert all object that containe IndependedData attributes
        /// </summary>
        /// <returns></returns>
        ISqlQueriable<T> SaveAll(Func<T, bool> match);
        /// <summary>
        /// Save object Herarkie, will Update or Insert all object that containe IndependedData attributes
        /// </summary>
        /// <returns></returns>
        Task<ISqlQueriable<T>> SaveAllAsync(Func<T, bool> match);
        /// <summary>
        /// will remove and Save all items from the database
        /// this will delete all data Herarkie, make sure that the object containe all the children.
        /// it wont remove object that containe Independed attributes
        /// </summary>
        void Remove();
        /// <summary>
        /// will remove and Save all items from the database
        /// this will delete all data Herarkie, make sure that the object containe all the children.
        /// it wont remove object that containe Independed attributes
        /// </summary>
        Task RemoveAsync();
        /// <summary>
        /// will remove and Save all items from the database
        /// this will delete all data Herarkie, make sure that the object containe all the children.
        /// it wont remove object that containe Independed attributes
        /// </summary>
        Task RemoveAllAsync(Func<T, bool> match);
        /// <summary>
        /// will remove and Save all selected items from the DataBase
        /// </summary>
        /// <param name="match"></param>
        void RemoveAll(Func<T, bool> match);
        /// <summary>
        /// Pager take
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        ISqlQueriable<T> Take(int value);
        /// <summary>
        /// Paget Skip, must be compained with Take 
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        ISqlQueriable<T> Skip(int value);
        /// <summary>
        /// OrderBy column
        /// </summary>
        /// <param name="exp"></param>
        /// <returns></returns>
        ISqlQueriable<T> OrderBy(Expression<Func<T, object>> exp);
        /// <summary>
        /// OrderByDescending column
        /// </summary>
        /// <param name="exp"></param>
        /// <returns></returns>
        ISqlQueriable<T> OrderByDescending(Expression<Func<T, object>> exp);
        /// <summary>
        /// Convert type to another type
        /// make sure that all property are mapped by inc [PropertyName] over each property
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <returns></returns>
        List<TSource> ExecuteAndConvertToType<TSource>() where TSource : class;
        /// <summary>
        /// Dispose TransactionData
        /// </summary>
        void Dispose();
    }
}
