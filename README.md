# ScrumApplication

## Opis projektu
ScrumApplication to aplikacja webowa do zarządzania zadaniami i wydarzeniami w stylu Scrum.  
Umożliwia tworzenie, edytowanie i usuwanie zarówno wydarzeń jak i zadań (zawsze przypisanych do wybranego wydarzenia).  
Aplikacja umożliwia również kontrolę statusów wydarzeń oraz zadań (W trakcie/Wykonane).  
ScrumApplication posiada system autoryzacji z rolami użytkowników (Admin, User).

## Funkcje
- Dodawanie, edytowanie i usuwanie zadań oraz wydarzeń  
- Przypisywanie zadań do wydarzeń Scrum 
- Zmiana statusu zadań i wydarzeń (W trakcie, Wykonane)  
- Rejestracja i logowanie użytkowników z przypisaniem ról  
- Automatyczne testy UI realizowane za pomocą Selenium  

## Technologie
- .NET 8.0 (net8.0)  
- ASP.NET Core 8.0 z Identity (Microsoft.AspNetCore.Identity.EntityFrameworkCore 8.0.19)  
- Entity Framework Core 9.0.8 z SQL Server  
- SignalR 1.2.0  
- Selenium WebDriver 4.35.0 z ChromeDriver do automatycznych testów UI  
- xUnit 2.9.3 do testów jednostkowych 

## Instrukcja uruchomienia
1. Sklonuj repozytorium:  
   `git clone https://github.com/jpurgat95/ScrumApplication`  
2. Skonfiguruj połączenie do bazy danych w `appsettings.json` (np. SQL Server).  
3. Wykonaj migracje bazy danych przy pomocy Entity Framework Core:  
   `dotnet ef database update`  
4. Uruchom aplikację:  
   `dotnet run --project ScrumApplication/ScrumApplication.csproj`  
5. Uruchom testy:  
   `dotnet test ScrumApplicationTests/ScrumApplicationTests.csproj`  

## Testy
Projekt zawiera testy automatyczne Selenium w projekcie `ScrumApplicationTests`. Zaleca się uruchamiać testy pojedynczo lub odpowiednio konfigurować synchronizację przy testach równoległych.

## Diagram ERD bazy danych
[`Diagram ERD`](https://github.com/user-attachments/assets/d4227edb-4ffc-4f71-9f0e-055567bf6399)  
- **AspNetUsers** — użytkownicy (PK: Id)  
- **Events** — wydarzenia, z kluczem obcym `UserId` do `AspNetUsers` (1 użytkownik → wiele wydarzeń)  
- **Tasks** — zadania, z kluczami obcymi `UserId` do `AspNetUsers` oraz `ScrumEventId` do `Events` (1 wydarzenie → wiele zadań)  
- **AspNetRoles** — role użytkowników (Admin, User)  
- **AspNetUserRoles** — łączenie użytkowników z rolami (wiele-do-wielu) poprzez `UserId` i `RoleId`

## Użytkownicy

Projekt zawiera dwa podstawowe typy użytkowników:

- **Admin**  
  - Ma dostęp do wszystkich wydarzeń i zadań — zarówno swoich, jak i innych użytkowników.  
  - Może zarządzać kontami użytkowników w panelu administracyjnym:  
    - Wymuszać zmianę hasła przez użytkownika.  
    - Usuwać użytkowników wraz z powiązanymi danymi (zadania, wydarzenia).  

- **User**  
  - Ma dostęp wyłącznie do swoich własnych wydarzeń i zadań.  
  - Nie ma uprawnień administracyjnych.

## Screeny i filmik z działania
- **Screeny** 
  - [`Niezalogowany użytkownik`](https://github.com/user-attachments/assets/041a91dd-700a-4f17-9b30-906de7030d8d)
  - [`Zalogowany User`](https://github.com/user-attachments/assets/35116da4-1bb1-41dc-9dc9-c1c021d35468)
  - [`Wydarzenia - widok Usera`](https://github.com/user-attachments/assets/8ef810cd-fbc4-43c0-9e4e-3aa7b843b93e)
  - [`Zadania - widok Usera`](https://github.com/user-attachments/assets/72aa6cba-3f00-42c9-921b-87d1ba9e673d)
  - [`Zalogowany Admin`](https://github.com/user-attachments/assets/86ea4ebc-b64a-4900-9dac-f4f14890f764)
  - [`Wydarzenia - widok Admina - część 1`](https://github.com/user-attachments/assets/6e01c0a9-a704-4285-994e-608a686f56ba) , [`Wydarzenia - widok Admina - część 2`](https://github.com/user-attachments/assets/e9e179e5-b4bc-4fa9-b231-45e897a7c70f)
  - [`Zadania - widok Admina - część 1`](https://github.com/user-attachments/assets/9cb6cda4-2f9d-4663-a4f1-24a80ee9a709) , [`Zadania - widok Admina - część 2`](https://github.com/user-attachments/assets/30e8cb54-88fc-40ca-a8c1-b54649f7c6c9)
- **Filmik**
- [`Prezentacja działania aplikacji na YT`](https://youtu.be/ohjLu25dHjI)

## Kontakt
Autor: Jarosław Purgat  
Email: j.purgat95@gmail.com
