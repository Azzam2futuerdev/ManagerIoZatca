namespace ZatcaApi.Repositories
{
    public interface IRepository<T>
    {
        Task<IEnumerable<T>> GetAll();
        Task<T> GetById(int id);
        Task Update(T entity);
        //Task Delete(int id);
        Task<IEnumerable<T>> GetPaged(int page, int pageSize);
    }
}
