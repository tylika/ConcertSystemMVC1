using Microsoft.AspNetCore.Identity;

namespace ConcertSystemDomain.Model
{
    public class ApplicationUser : IdentityUser
    {
        // Додаткові поля, якщо потрібні (наприклад, ім’я)
        public string FullName { get; set; }
    }
}