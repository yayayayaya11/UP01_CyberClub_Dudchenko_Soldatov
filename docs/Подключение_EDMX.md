# Подключение через ADO.NET Entity Data Model

Проект переведён на **Entity Framework 6.4.4, Database First** с моделью `Models/CyberClubModel.edmx`. Существующая база и её данные не пересоздаются.

## Где находится модель

Откройте `CyberClub.sln` в Visual Studio и раскройте папку Models. Дважды щёлкните `CyberClubModel.edmx`: откроется дизайнер модели с восемью сущностями и шестью связями.

| Файл | Назначение |
|---|---|
| CyberClubModel.edmx | Концептуальная модель, структура БД, сопоставление полей и диаграмма. |
| CyberClubModel.Context.cs | Контекст CyberClubEntities с восемью DbSet. |
| CyberClubModel.tt | T4-шаблон, создающий классы сущностей из модели. |
| CyberClubModel.cs | Результат генерации: User, Client, Club, Zone, Computer, Tariff, Booking, Payment. |
| App.config | Именованная EntityClient-строка подключения CyberClubEntities и настройка провайдера EF6. |
| packages.config | Версия пакета EntityFramework 6.4.4. DLL уже включены в packages и копируются при сборке. |

В свойстве действия сборки EDMX указано **EntityDeploy**. Стандартная сборка Visual Studio извлекает CSDL, SSDL и MSL из модели и встраивает их в EXE. Скрипт build.ps1 выполняет такое же извлечение перед вызовом компилятора.

## Подключение

- Сервер: `(localdb)\MSSQLLocalDB`.
- База: `DB_Dudchenko_Soldatov_CyberClub`.
- Аутентификация: Windows.
- Имя подключения: `CyberClubEntities`.
- Внешний провайдер: `System.Data.EntityClient`.
- Провайдер SQL Server: `System.Data.SqlClient` внутри EntityClient-строки.

В App.config важно сохранить как часть `metadata=res://*/...`, так и `provider connection string`. Подмена всей строки обычной SQL-строкой приведёт к попытке Code First, которую контекст намеренно запрещает.

Для уже собранного приложения настройки находятся в CyberClub.exe.config. Правка App.config применяется после пересборки.

## Использование контекста

```csharp
public static class Core
{
    public static CyberClubEntities Context = new CyberClubEntities();
}

var clubs = Core.Context.Clubs.AsNoTracking().ToList();
var club = Core.Context.Clubs.Find(id);
club.Address = "Новый адрес";
Core.SaveChanges();
```

`src/Core.cs` содержит общий контекст `Core.Context`. Авторизация выполняет LINQ-запрос к его Users. Регистрация вызывает Users.Add, смена пароля меняет свойство сущности. Core.SaveChanges вызывает Context.SaveChanges и очищает отслеживание после успешной или отклонённой операции. Это предотвращает повторную отправку ошибочных записей. Класс DataContext удалён.

Для общих экранов DataGrid и аналитических объединений оставлены параметризованные табличные запросы. Они получают соединение из CyberClubEntities, а не создают отдельное подключение приложения. Прямой SqlConnection в установке и проверках нужен только для создания и удаления отдельной тестовой базы в master.

## Обновление модели после изменения БД

1. Откройте EDMX в дизайнере Visual Studio.
2. Выберите «Обновить модель из базы данных» в контекстном меню и используйте подключение к этой LocalDB.
3. Сохраните EDMX.
4. Для `CyberClubModel.tt` выполните «Запустить пользовательский инструмент» (Run Custom Tool). Он обновит CyberClubModel.cs.
5. Если добавлялись новые таблицы, добавьте соответствующие DbSet в контекст и описание раздела в Catalog. Для существующих восьми таблиц всё уже сделано.
6. Пересоберите решение, запустите CyberClub.exe и проверьте вход, просмотр разделов и сохранение записей.

Автоматическое создание и миграции Code First отключены. Каскады, ограничения и триггеры продолжают выполняться SQL Server. Общий контекст создаётся в Core и освобождается при выходе из приложения. Его нельзя оборачивать в using при выполнении отдельных операций.
