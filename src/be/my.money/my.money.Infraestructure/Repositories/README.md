# Ejemplo de uso del Repositorio Genérico y Unit of Work

## Patrón Repository implementado

Este proyecto utiliza el patrón Repository genérico con Unit of Work para abstraer el acceso a datos.

### Estructura

```
my.money.domain/
  ??? Interfaces/
      ??? IRepository.cs      (Repositorio genérico)
      ??? IUnitOfWork.cs      (Unit of Work)

my.money.Infraestructure/
  ??? Repositories/
      ??? Repository.cs       (Implementación genérica)
      ??? UnitOfWork.cs       (Implementación Unit of Work)
```

### Ventajas

? Desacoplamiento del acceso a datos
? Facilita el testing con mocks
? Operaciones transaccionales centralizadas
? Código más limpio y mantenible
? Reutilización de código

### Uso Básico

```csharp
// Inyectar en el constructor
public class UsersController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    
    public UsersController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }
    
    // Obtener todos
    var users = await _unitOfWork.Users.GetAllAsync();
    
    // Buscar por criterio
    var activeUsers = await _unitOfWork.Users.FindAsync(u => u.IsActive);
    
    // Agregar
    await _unitOfWork.Users.AddAsync(newUser);
    await _unitOfWork.SaveChangesAsync();
    
    // Actualizar
    _unitOfWork.Users.Update(user);
    await _unitOfWork.SaveChangesAsync();
    
    // Eliminar
    _unitOfWork.Users.Remove(user);
    await _unitOfWork.SaveChangesAsync();
}
```

### Uso con Transacciones

```csharp
public async Task<IActionResult> CreateMultipleUsers(List<CreateUserDto> usersDto)
{
    try
    {
        await _unitOfWork.BeginTransactionAsync();
        
        foreach (var userDto in usersDto)
        {
            var user = new User 
            { 
                Name = userDto.Name, 
                Email = userDto.Email 
            };
            await _unitOfWork.Users.AddAsync(user);
        }
        
        await _unitOfWork.SaveChangesAsync();
        await _unitOfWork.CommitTransactionAsync();
        
        return Ok();
    }
    catch (Exception)
    {
        await _unitOfWork.RollbackTransactionAsync();
        throw;
    }
}
```

### Métodos Disponibles

**IRepository<T>**
- `GetByIdAsync(int id)` - Obtener por ID
- `GetAllAsync()` - Obtener todos
- `FindAsync(Expression<Func<T, bool>> predicate)` - Buscar con filtro
- `FirstOrDefaultAsync(Expression<Func<T, bool>> predicate)` - Primer elemento o null
- `AnyAsync(Expression<Func<T, bool>> predicate)` - Verificar existencia
- `AddAsync(T entity)` - Agregar entidad
- `AddRangeAsync(IEnumerable<T> entities)` - Agregar múltiples
- `Update(T entity)` - Actualizar
- `Remove(T entity)` - Eliminar
- `RemoveRange(IEnumerable<T> entities)` - Eliminar múltiples

**IUnitOfWork**
- `Users` - Repositorio de usuarios
- `SaveChangesAsync()` - Guardar cambios
- `BeginTransactionAsync()` - Iniciar transacción
- `CommitTransactionAsync()` - Confirmar transacción
- `RollbackTransactionAsync()` - Revertir transacción

### Agregar Nuevas Entidades

Para agregar una nueva entidad al Unit of Work:

1. Crear la interfaz en `my.money.domain/Interfaces/IUnitOfWork.cs`:
```csharp
public interface IUnitOfWork : IDisposable
{
    IRepository<User> Users { get; }
    IRepository<Category> Categories { get; } // Nueva entidad
    Task<int> SaveChangesAsync();
}
```

2. Implementar en `UnitOfWork.cs`:
```csharp
private IRepository<Category>? _categories;

public IRepository<Category> Categories
{
    get
    {
        _categories ??= new Repository<Category>(_context);
        return _categories;
    }
}
```

3. Usar en los controladores:
```csharp
var categories = await _unitOfWork.Categories.GetAllAsync();
```
