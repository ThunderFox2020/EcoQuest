using EcoQuest.Models;
using EcoQuest.Models.DTO;
using EcoQuest.Models.Entities;
using System.Globalization;

namespace EcoQuest.Services
{
    public static class RequestValidator
    {
        public static (bool, IResult) ValidateGameModel(eco_questContext db, Game request)
        {
            if (request.Name == null || request.Name == string.Empty)
                return (false, Results.BadRequest("Название игры не может иметь значение null"));
            if (request.Message == null || request.Message == string.Empty)
                return (false, Results.BadRequest("Приветственное сообщение игры не может иметь значение null"));
            if (request.Date == null || request.Date == string.Empty)
                return (false, Results.BadRequest("Дата проведения игры не может иметь значение null"));

            if (!DateTime.TryParse(request.Date, new CultureInfo("en-US"), DateTimeStyles.None, out _))
                return (false, Results.BadRequest("Дата проведения игры имеет невалидный формат"));

            User? targetUser = (from u in db.Users
                                where u.UserId == request.UserId
                                select u).FirstOrDefault();

            if (targetUser == null)
                return (false, Results.NotFound("Запрашиваемый пользователь не найден"));
            if (!(targetUser.Role == "master" && targetUser.Status == "active"))
                return (false, Results.BadRequest("Запрашиваемый пользователь не является активным ведущим"));

            return (true, Results.Ok());
        }
        public static (bool, IResult) ValidateGameBoardModel(eco_questContext db, GameBoard request)
        {
            if (request.Name == null || request.Name == string.Empty)
                return (false, Results.BadRequest("Название шаблона не может иметь значение null"));

            User? targetUser = (from u in db.Users
                                where u.UserId == request.UserId
                                select u).FirstOrDefault();

            if (targetUser == null)
                return (false, Results.NotFound("Запрашиваемый пользователь не найден"));
            if (!(targetUser.Role == "master" && targetUser.Status == "active"))
                return (false, Results.BadRequest("Запрашиваемый пользователь не является активным ведущим"));

            List<long> allProductIds = (from p in db.Products
                                        select p.ProductId).ToList();
            List<long> allQuestionIds = (from q in db.Questions
                                         select q.QuestionId).ToList();

            foreach (var gameBoardsProduct in request.GameBoardsProducts)
            {
                if (!allProductIds.Contains(gameBoardsProduct.ProductId))
                    return (false, Results.NotFound("Один или несколько продуктов не найдены"));
            }
            foreach (var gameBoardsQuestion in request.Questions)
            {
                if (!allQuestionIds.Contains(gameBoardsQuestion.QuestionId))
                    return (false, Results.NotFound("Один или несколько вопросов не найдены"));
            }

            return (true, Results.Ok());
        }
        public static (bool, IResult) ValidateProductModel(eco_questContext db, Product request)
        {
            if (request.Colour == null || request.Colour == string.Empty)
                return (false, Results.BadRequest("Цвет продукта не может иметь значение null"));
            if (request.Name == null || request.Name == string.Empty)
                return (false, Results.BadRequest("Название продукта не может иметь значение null"));

            Product? targetProduct = (from p in db.Products
                                      where p.ProductId != request.ProductId && p.Name == request.Name
                                      select p).FirstOrDefault();

            if (targetProduct != null)
                return (false, Results.BadRequest("Запрашиваемое название продукта уже существует"));

            return (true, Results.Ok());
        }
        public static (bool, IResult) ValidateStatisticModel(eco_questContext db, Statistic request)
        {
            if (request.Date == null || request.Date == string.Empty)
                return (false, Results.BadRequest("Дата проведения игры не может иметь значение null"));
            if (request.Duration == null || request.Duration == string.Empty)
                return (false, Results.BadRequest("Продолжительность игры не может иметь значение null"));
            if (request.Results == null || request.Results == string.Empty)
                return (false, Results.BadRequest("Результаты игры не могут иметь значение null"));
            if (!DateTime.TryParse(request.Date, new CultureInfo("en-US"), DateTimeStyles.None, out _))
                return (false, Results.BadRequest("Дата проведения игры имеет невалидный формат"));
            if (!TimeSpan.TryParse(request.Duration, new CultureInfo("en-US"), out _))
                return (false, Results.BadRequest("Продолжительность игры имеет невалидный формат"));

            User? targetUser = (from u in db.Users
                                where u.UserId == request.UserId
                                select u).FirstOrDefault();

            if (targetUser == null)
                return (false, Results.NotFound("Запрашиваемый пользователь не найден"));
            if (!(targetUser.Role == "master" && targetUser.Status == "active"))
                return (false, Results.BadRequest("Запрашиваемый пользователь не является активным ведущим"));

            request.LastName = targetUser.LastName;
            request.FirstName = targetUser.FirstName;
            request.Patronymic = targetUser.Patronymic;
            request.Login = targetUser.Login;

            return (true, Results.Ok());
        }
        public static (bool, IResult) ValidateUserModel(eco_questContext db, User request)
        {
            if (request.LastName == null || request.LastName == string.Empty)
                return (false, Results.BadRequest("Фамилия пользователя не может иметь значение null"));
            if (request.FirstName == null || request.FirstName == string.Empty)
                return (false, Results.BadRequest("Имя пользователя не может иметь значение null"));
            if (request.Patronymic == null || request.Patronymic == string.Empty)
                return (false, Results.BadRequest("Отчество пользователя не может иметь значение null"));
            if (request.Login == null || request.Login == string.Empty)
                return (false, Results.BadRequest("Логин пользователя не может иметь значение null"));
            if (request.Password == null || request.Password == string.Empty)
                return (false, Results.BadRequest("Пароль пользователя не может иметь значение null"));

            User? targetUser = (from u in db.Users
                                where u.UserId != request.UserId && u.Login == request.Login
                                select u).FirstOrDefault();

            if (targetUser != null)
                return (false, Results.BadRequest("Запрашиваемый логин пользователя уже существует"));

            return (true, Results.Ok());
        }
        public static (bool, IResult) ValidateLoginMasterDTO(eco_questContext db, LoginMasterDTO request)
        {
            if (request.Login == null || request.Login == string.Empty)
                return (false, Results.BadRequest("Логин пользователя не может иметь значение null"));
            if (request.Password == null || request.Password == string.Empty)
                return (false, Results.BadRequest("Пароль пользователя не может иметь значение null"));

            return (true, Results.Ok());
        }
        public static (bool, IResult) ValidateLoginPlayerDTO(eco_questContext db, LoginPlayerDTO request)
        {
            if (request.Login == null || request.Login == string.Empty)
                return (false, Results.BadRequest("Логин пользователя не может иметь значение null"));

            return (true, Results.Ok());
        }
        public static (bool, IResult) ValidateProductExportDTO(eco_questContext db, ProductExportDTO request)
        {
            List<long> allProductIds = (from p in db.Products
                                        select p.ProductId).ToList();

            foreach (var productId in request.ProductIds)
            {
                if (!allProductIds.Contains(productId))
                    return (false, Results.NotFound("Один или несколько продуктов не найдены"));
            }

            if (request.FileName == null || request.FileName == string.Empty)
                return (false, Results.BadRequest("Имя файла не может иметь значение null"));

            return (true, Results.Ok());
        }
        public static (bool, IResult) ValidateUpdatePasswordDTO(eco_questContext db, UpdatePasswordDTO request)
        {
            if (request.Login == null || request.Login == string.Empty)
                return (false, Results.BadRequest("Логин пользователя не может иметь значение null"));
            if (request.OldPassword == null || request.OldPassword == string.Empty)
                return (false, Results.BadRequest("Старый пароль пользователя не может иметь значение null"));
            if (request.NewPassword == null || request.NewPassword == string.Empty)
                return (false, Results.BadRequest("Новый пароль пользователя не может иметь значение null"));

            return (true, Results.Ok());
        }
    }
}