using ClosedXML.Excel;
using EcoQuest.Models;
using EcoQuest.Models.DTO;
using EcoQuest.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Text.Unicode;

namespace EcoQuest.Services
{
    public class ApplicationService
    {
        public ApplicationService(WebApplication app)
        {
            _app = app;
        }

        private readonly WebApplication _app;

        public IResult LoginMaster(eco_questContext db, LoginMasterDTO request)
        {
            (bool, IResult) validResult = RequestValidator.ValidateLoginMasterDTO(db, request);

            if (!validResult.Item1)
                return validResult.Item2;

            User? targetUser = (from u in db.Users
                                where u.Login == request.Login
                                select u).FirstOrDefault();

            if (targetUser == null)
                return Results.NotFound("Пользователь с запрашиваемым логином не найден");
            if (targetUser.Password != PasswordHasher.Encrypt(request.Password))
                return Results.Unauthorized();

            List<Claim> claims = new List<Claim>()
            {
                new Claim(ClaimTypes.Name, targetUser.Login),
                new Claim(ClaimTypes.Role, $"{targetUser.Role + targetUser.Status}")
            };

            JwtSecurityToken JWT = new JwtSecurityToken(
                issuer: AuthenticationOptions.ISSUER,
                audience: AuthenticationOptions.AUDIENCE,
                claims: claims,
                expires: DateTime.UtcNow.Add(TimeSpan.FromDays(30)),
                signingCredentials: new SigningCredentials(AuthenticationOptions.GetSymmetricSecurityKey(), SecurityAlgorithms.HmacSha256));

            string encodedJWT = new JwtSecurityTokenHandler().WriteToken(JWT);

            return Results.Json(new
            {
                targetUser.UserId,
                targetUser.Login,
                targetUser.Role,
                targetUser.Status,
                AuthorizationToken = encodedJWT
            });
        }
        public IResult LoginPlayer(eco_questContext db, LoginPlayerDTO request)
        {
            DeleteExpiredGames(db);

            (bool, IResult) validResult = RequestValidator.ValidateLoginPlayerDTO(db, request);

            if (!validResult.Item1)
                return validResult.Item2;

            Game? targetGame = (from g in db.Games
                                where g.GameId == request.GameId
                                select g).FirstOrDefault();

            if (targetGame == null)
                return Results.NotFound("Запрашиваемая игра не найдена");

            List<Claim> claims = new List<Claim>()
            {
                new Claim(ClaimTypes.Name, request.Login),
                new Claim(ClaimTypes.Role, "player")
            };

            JwtSecurityToken JWT = new JwtSecurityToken(
                issuer: AuthenticationOptions.ISSUER,
                audience: AuthenticationOptions.AUDIENCE,
                claims: claims,
                expires: DateTime.UtcNow.Add(TimeSpan.FromDays(30)),
                signingCredentials: new SigningCredentials(AuthenticationOptions.GetSymmetricSecurityKey(), SecurityAlgorithms.HmacSha256));

            string encodedJWT = new JwtSecurityTokenHandler().WriteToken(JWT);

            return Results.Json(new
            {
                request.GameId,
                request.Login,
                Role = "player",
                AuthorizationToken = encodedJWT
            });
        }

        public IResult CreateGame(eco_questContext db, Game request)
        {
            DeleteExpiredGames(db);

            (bool, IResult) validResult = RequestValidator.ValidateGameModel(db, request);

            if (!validResult.Item1)
                return validResult.Item2;

            List<long> allGameIds = (from g in db.Games
                                     select g.GameId).ToList();

            long gameId = 0;

            for (int id = 1; id <= 99999; id++)
            {
                if (!allGameIds.Contains(id))
                {
                    gameId = id;
                    break;
                }
            }

            if (gameId == 0)
                return Results.BadRequest("Пул игр переполнен");

            Game newGame = new Game()
            {
                GameId = gameId,
                UserId = request.UserId,
                Name = request.Name,
                Message = request.Message,
                Date = request.Date,
                State = request.State,
                CurrentQuestionId = request.CurrentQuestionId,
            };

            db.Games.Add(newGame);

            db.SaveChanges();

            return Results.Ok();
        }
        public IResult DeleteGameById(eco_questContext db, long id)
        {
            DeleteExpiredGames(db);

            Game? targetGame = (from g in db.Games
                                where g.GameId == id
                                select g).FirstOrDefault();

            if (targetGame != null)
                db.Games.Remove(targetGame);

            db.SaveChanges();

            return Results.Ok();
        }
        public IResult GetGameById(eco_questContext db, long id)
        {
            DeleteExpiredGames(db);

            Game? targetGame = (from g in db.Games
                                where g.GameId == id
                                select g).FirstOrDefault();

            if (targetGame == null)
                return Results.NotFound("Запрашиваемая игра не найдена");

            Question? targetQuestion = (from q in db.Questions
                                        where q.QuestionId == targetGame.CurrentQuestionId
                                        select q).FirstOrDefault();

            return Results.Json(new
            {
                targetGame.GameId,
                targetGame.UserId,
                targetGame.Name,
                targetGame.Message,
                targetGame.Date,
                targetGame.State,
                targetGame.CurrentQuestionId,
                CurrentQuestionAnswer = targetQuestion == null ? null : targetQuestion.Answers
            });
        }
        public IResult GetAllGames(eco_questContext db)
        {
            DeleteExpiredGames(db);

            List<Game> allGames = db.Games.ToList();

            foreach (var game in allGames)
            {
                game.CurrentQuestionId = null;
            }

            return Results.Json(allGames.OrderBy(x => x.GameId));
        }
        public IResult GetAllGamesByUserId(eco_questContext db, long id)
        {
            DeleteExpiredGames(db);

            User? targetUser = (from u in db.Users.Include(x => x.Games)
                                where u.UserId == id
                                select u).FirstOrDefault();

            if (targetUser == null)
                return Results.NotFound("Запрашиваемый пользователь не найден");
            if (!(targetUser.Role == "master" && targetUser.Status == "active"))
                return Results.BadRequest("Запрашиваемый пользователь не является активным ведущим");

            foreach (var game in targetUser.Games)
            {
                game.CurrentQuestionId = null;
                game.User = null;
            }

            return Results.Json(targetUser.Games.OrderBy(x => x.GameId));
        }
        public IResult CreatePlayerById(eco_questContext db, PlayerDTO request, long id)
        {
            DeleteExpiredGames(db);

            Game? targetGame = (from g in db.Games
                                where g.GameId == id
                                select g).FirstOrDefault();

            if (targetGame == null)
                return Results.NotFound("Запрашиваемая игра не найдена");
            if (targetGame.State == null)
                return Results.BadRequest("Поле State имеет значение null");

            JsonNode? stateNode = JsonNode.Parse(targetGame.State);

            if (stateNode != null)
            {
                JsonNode? playersNode = stateNode["Players"];

                if (playersNode != null)
                {
                    JsonArray allPlayers = playersNode.AsArray();

                    List<long> allPlayerIds = (from p in allPlayers
                                               select (long)p["PlayerId"]!).ToList();

                    long newPlayerId = 1;

                    while (allPlayerIds.Contains(newPlayerId))
                        newPlayerId++;

                    PlayerDTO newPlayer = new PlayerDTO()
                    {
                        PlayerId = newPlayerId,
                        Login = request.Login,
                        List = request.List
                    };

                    allPlayers.Add(newPlayer);

                    JsonObject stateObject = stateNode.AsObject();

                    stateObject["Players"] = allPlayers;

                    targetGame.State = stateObject.ToJsonString(new JsonSerializerOptions()
                    {
                        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
                        WriteIndented = true
                    });

                    db.SaveChanges();
                }
                else
                {
                    return Results.BadRequest("Поле Players имеет значение null");
                }
            }
            else
            {
                return Results.BadRequest("Поле State имеет значение null");
            }

            return Results.Ok();
        }
        public IResult DeletePlayerByGameIdAndPlayerId(eco_questContext db, long gameId, long playerId)
        {
            DeleteExpiredGames(db);

            Game? targetGame = (from g in db.Games
                                where g.GameId == gameId
                                select g).FirstOrDefault();

            if (targetGame == null)
                return Results.NotFound("Запрашиваемая игра не найдена");
            if (targetGame.State == null)
                return Results.BadRequest("Поле State имеет значение null");

            JsonNode? stateNode = JsonNode.Parse(targetGame.State);

            if (stateNode != null)
            {
                JsonNode? playersNode = stateNode["Players"];

                if (playersNode != null)
                {
                    JsonArray allPlayers = playersNode.AsArray();

                    JsonNode? targetPlayerNode = (from p in allPlayers
                                                  where (long)p["PlayerId"]! == playerId
                                                  select p).FirstOrDefault();

                    if (targetPlayerNode != null)
                    {
                        allPlayers.Remove(targetPlayerNode);

                        JsonObject stateObject = stateNode.AsObject();

                        stateObject["Players"] = allPlayers;

                        targetGame.State = stateObject.ToJsonString(new JsonSerializerOptions()
                        {
                            Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
                            WriteIndented = true
                        });

                        db.SaveChanges();
                    }
                }
                else
                {
                    return Results.BadRequest("Поле Players имеет значение null");
                }
            }
            else
            {
                return Results.BadRequest("Поле State имеет значение null");
            }

            return Results.Ok();
        }
        public IResult UpdatePlayerById(eco_questContext db, PlayerDTO request, long id)
        {
            DeleteExpiredGames(db);

            Game? targetGame = (from g in db.Games
                                where g.GameId == id
                                select g).FirstOrDefault();

            if (targetGame == null)
                return Results.NotFound("Запрашиваемая игра не найдена");
            if (targetGame.State == null)
                return Results.BadRequest("Поле State имеет значение null");

            JsonNode? stateNode = JsonNode.Parse(targetGame.State);

            if (stateNode != null)
            {
                JsonNode? playersNode = stateNode["Players"];

                if (playersNode != null)
                {
                    JsonArray allPlayers = playersNode.AsArray();

                    JsonNode? targetPlayerNode = (from p in allPlayers
                                                  where (long)p["PlayerId"]! == request.PlayerId
                                                  select p).FirstOrDefault();

                    if (targetPlayerNode == null)
                        return Results.NotFound("Запрашиваемый игрок не найден");

                    JsonObject targetPlayerObject = targetPlayerNode.AsObject();

                    targetPlayerObject["Login"] = request.Login;
                    targetPlayerObject["List"] = request.List;

                    targetGame.State = stateNode.ToJsonString(new JsonSerializerOptions()
                    {
                        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
                        WriteIndented = true
                    });

                    db.SaveChanges();
                }
                else
                {
                    return Results.BadRequest("Поле Players имеет значение null");
                }
            }
            else
            {
                return Results.BadRequest("Поле State имеет значение null");
            }

            return Results.Ok();
        }
        public IResult UpdateGame(eco_questContext db, Game request)
        {
            DeleteExpiredGames(db);

            (bool, IResult) validResult = RequestValidator.ValidateGameModel(db, request);

            if (!validResult.Item1)
                return validResult.Item2;

            Game? targetGame = (from g in db.Games
                                where g.GameId == request.GameId
                                select g).FirstOrDefault();

            if (targetGame == null)
                return Results.NotFound("Запрашиваемая игра не найдена");

            targetGame.UserId = request.UserId;
            targetGame.Name = request.Name;
            targetGame.Message = request.Message;
            targetGame.Date = request.Date;
            targetGame.State = request.State;
            targetGame.CurrentQuestionId = request.CurrentQuestionId;

            db.SaveChanges();

            return Results.Ok();
        }
        public IResult UpdateStateAndQuestion(eco_questContext db, Game request)
        {
            DeleteExpiredGames(db);

            Game? targetGame = (from g in db.Games
                                where g.GameId == request.GameId
                                select g).FirstOrDefault();

            if (targetGame == null)
                return Results.NotFound("Запрашиваемая игра не найдена");

            targetGame.State = request.State;
            targetGame.CurrentQuestionId = request.CurrentQuestionId;

            db.SaveChanges();

            return Results.Ok();
        }

        public IResult CreateGameBoard(eco_questContext db, GameBoardDTO request)
        {
            GameBoard convertedRequest = ModelConverter.ToGameBoard(db, request);

            (bool, IResult) validResult = RequestValidator.ValidateGameBoardModel(db, convertedRequest);

            if (!validResult.Item1)
                return validResult.Item2;

            GameBoard newGameBoard = new GameBoard()
            {
                Name = convertedRequest.Name,
                NumFields = convertedRequest.NumFields,
                UserId = convertedRequest.UserId,
            };

            foreach (var gameBoardsProduct in convertedRequest.GameBoardsProducts)
            {
                GameBoardsProduct newGameBoardsProduct = new GameBoardsProduct()
                {
                    ProductId = gameBoardsProduct.ProductId,
                    NumOfRepeating = gameBoardsProduct.NumOfRepeating
                };

                newGameBoard.GameBoardsProducts.Add(newGameBoardsProduct);
            }

            List<Question> allQuestions = db.Questions.ToList();

            foreach (var gameBoardsQuestion in convertedRequest.Questions)
            {
                Question? targetQuestion = (from q in allQuestions
                                            where q.QuestionId == gameBoardsQuestion.QuestionId
                                            select q).FirstOrDefault();

                if (targetQuestion != null)
                    newGameBoard.Questions.Add(targetQuestion);
            }

            db.GameBoards.Add(newGameBoard);

            db.SaveChanges();

            return Results.Ok();
        }
        public IResult DeleteGameBoardById(eco_questContext db, long id)
        {
            GameBoard? targetGameBoard = (from gb in db.GameBoards
                                          where gb.GameBoardId == id
                                          select gb).FirstOrDefault();

            if (targetGameBoard != null)
                db.GameBoards.Remove(targetGameBoard);

            db.SaveChanges();

            return Results.Ok();
        }
        public IResult GetGameBoardById(eco_questContext db, long id)
        {
            GameBoard? targetGameBoard = (from gb in db.GameBoards.Include(x => x.GameBoardsProducts).ThenInclude(y => y.Product).Include(z => z.Questions)
                                          where gb.GameBoardId == id
                                          select gb).FirstOrDefault();

            if (targetGameBoard == null)
                return Results.NotFound("Запрашиваемый шаблон не найден");

            foreach (var gameBoardsProduct in targetGameBoard.GameBoardsProducts)
            {
                List<Question> targetQuestions = (from q in targetGameBoard.Questions
                                                  where q.ProductId == gameBoardsProduct.ProductId
                                                  select q).ToList();

                foreach (var question in targetQuestions)
                    gameBoardsProduct.Product.Questions.Add(question);
            }

            GameBoardDTO convertedResponse = ModelConverter.ToGameBoardDTO(db, targetGameBoard);

            return Results.Json(convertedResponse, new JsonSerializerOptions()
            {
                Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                ReferenceHandler = ReferenceHandler.IgnoreCycles,
                WriteIndented = true
            });
        }
        public IResult GetAllGameBoards(eco_questContext db)
        {
            List<GameBoard> allGameBoards = db.GameBoards.ToList();

            List<GameBoardDTO> convertedResponse = (from gb in allGameBoards
                                                    select ModelConverter.ToGameBoardDTO(db, gb)).ToList();

            return Results.Json(convertedResponse.OrderBy(x => x.GameBoardId));
        }
        public IResult GetAllGameBoardsByUserId(eco_questContext db, long id)
        {
            User? targetUser = (from u in db.Users.Include(x => x.GameBoards)
                                where u.UserId == id
                                select u).FirstOrDefault();

            if (targetUser == null)
                return Results.NotFound("Запрашиваемый пользователь не найден");
            if (!(targetUser.Role == "master" && targetUser.Status == "active"))
                return Results.BadRequest("Запрашиваемый пользователь не является активным ведущим");

            foreach (var gameBoard in targetUser.GameBoards)
                gameBoard.User = null;

            List<GameBoardDTO> convertedResponse = (from gb in targetUser.GameBoards
                                                    select ModelConverter.ToGameBoardDTO(db, gb)).ToList();

            return Results.Json(convertedResponse.OrderBy(x => x.GameBoardId));
        }
        public IResult ShareGameBoard(eco_questContext db, long fromUserId, long gameBoardId, long toUserId)
        {
            User? targetFromUser = (from u in db.Users
                                    where u.UserId == fromUserId
                                    select u).FirstOrDefault();

            if (targetFromUser == null)
                return Results.NotFound("Запрашиваемый пользователь адресант не найден");
            if (!(targetFromUser.Role == "master" && targetFromUser.Status == "active"))
                return Results.BadRequest("Запрашиваемый пользователь адресант не является активным ведущим");

            GameBoard? targetGameBoard = (from gb in db.GameBoards.Include(x => x.GameBoardsProducts).Include(y => y.Questions)
                                          where gb.GameBoardId == gameBoardId
                                          select gb).FirstOrDefault();

            if (targetGameBoard == null)
                return Results.NotFound("Запрашиваемый шаблон не найден");
            if (targetGameBoard.UserId != targetFromUser.UserId)
                return Results.BadRequest("Запрашиваемый шаблон не связан с запрашиваемым пользователем адресантом");

            User? targetToUser = (from u in db.Users
                                  where u.UserId == toUserId
                                  select u).FirstOrDefault();

            if (targetToUser == null)
                return Results.NotFound("Запрашиваемый пользователь адресат не найден");
            if (!(targetToUser.Role == "master" && targetToUser.Status == "active"))
                return Results.BadRequest("Запрашиваемый пользователь адресат не является активным ведущим");
            if (targetFromUser.UserId == targetToUser.UserId)
                return Results.BadRequest("Нельзя поделиться шаблоном с самим собой");

            GameBoard newGameBoard = new GameBoard()
            {
                Name = targetGameBoard.Name,
                NumFields = targetGameBoard.NumFields,
                UserId = targetToUser.UserId,
            };

            foreach (var gameBoardsProduct in targetGameBoard.GameBoardsProducts)
            {
                GameBoardsProduct newGameBoardsProduct = new GameBoardsProduct()
                {
                    ProductId = gameBoardsProduct.ProductId,
                    NumOfRepeating = gameBoardsProduct.NumOfRepeating
                };

                newGameBoard.GameBoardsProducts.Add(newGameBoardsProduct);
            }

            List<Question> allQuestions = db.Questions.ToList();

            foreach (var gameBoardsQuestion in targetGameBoard.Questions)
            {
                Question? targetQuestion = (from q in allQuestions
                                            where q.QuestionId == gameBoardsQuestion.QuestionId
                                            select q).FirstOrDefault();

                if (targetQuestion != null)
                    newGameBoard.Questions.Add(targetQuestion);
            }

            db.GameBoards.Add(newGameBoard);

            db.SaveChanges();

            return Results.Ok();
        }
        public IResult UpdateGameBoard(eco_questContext db, GameBoardDTO request)
        {
            GameBoard convertedRequest = ModelConverter.ToGameBoard(db, request);

            (bool, IResult) validResult = RequestValidator.ValidateGameBoardModel(db, convertedRequest);

            if (!validResult.Item1)
                return validResult.Item2;

            GameBoard? targetGameBoard = (from gb in db.GameBoards.Include(x => x.GameBoardsProducts).Include(y => y.Questions)
                                          where gb.GameBoardId == convertedRequest.GameBoardId
                                          select gb).FirstOrDefault();

            if (targetGameBoard == null)
            {
                DeleteGameBoardById(db, convertedRequest.GameBoardId);
                return CreateGameBoard(db, request);
            }

            targetGameBoard.Name = convertedRequest.Name;
            targetGameBoard.NumFields = convertedRequest.NumFields;
            targetGameBoard.UserId = convertedRequest.UserId;

            targetGameBoard.GameBoardsProducts.Clear();
            targetGameBoard.Questions.Clear();

            db.SaveChanges();

            foreach (var gameBoardsProduct in convertedRequest.GameBoardsProducts)
            {
                GameBoardsProduct newGameBoardsProduct = new GameBoardsProduct()
                {
                    GameBoardId = gameBoardsProduct.GameBoardId,
                    ProductId = gameBoardsProduct.ProductId,
                    NumOfRepeating = gameBoardsProduct.NumOfRepeating
                };

                targetGameBoard.GameBoardsProducts.Add(newGameBoardsProduct);
            }

            List<Question> allQuestions = db.Questions.ToList();

            foreach (var gameBoardsQuestion in convertedRequest.Questions)
            {
                Question? targetQuestion = (from q in allQuestions
                                            where q.QuestionId == gameBoardsQuestion.QuestionId
                                            select q).FirstOrDefault();

                if (targetQuestion != null)
                    targetGameBoard.Questions.Add(targetQuestion);
            }

            db.SaveChanges();

            return Results.Ok();
        }

        public IResult CreateProduct(eco_questContext db, Product request)
        {
            (bool, IResult) validResult = RequestValidator.ValidateProductModel(db, request);

            if (!validResult.Item1)
                return validResult.Item2;

            Product newProduct = new Product()
            {
                Colour = request.Colour,
                Name = request.Name,
                Round = request.Round,
                Logo = request.Logo
            };

            foreach (var question in request.Questions)
            {
                Question newQuestion = new Question()
                {
                    Answers = question.Answers,
                    Type = question.Type,
                    ShortText = question.ShortText,
                    Text = question.Text,
                    Media = question.Media,
                    LastEditDate = question.LastEditDate,
                };

                if (newQuestion.Type != "MEDIA")
                    newQuestion.Media = null;
                newQuestion.LastEditDate = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Ekaterinburg Standard Time")).ToString(new CultureInfo("en-US"));

                newProduct.Questions.Add(newQuestion);
            }

            db.Products.Add(newProduct);

            db.SaveChanges();

            return Results.Ok();
        }
        public IResult ExportProducts(eco_questContext db, ProductExportDTO request)
        {
            (bool, IResult) validResult = RequestValidator.ValidateProductExportDTO(db, request);

            if (!validResult.Item1)
                return validResult.Item2;

            List<Product> targetProducts;

            if (request.ProductIds.Count == 0)
            {
                targetProducts = db.Products.OrderBy(x => x.ProductId).Include(y => y.Questions.OrderBy(z => z.QuestionId)).ToList();
            }
            else
            {
                targetProducts = (from p in db.Products.OrderBy(x => x.ProductId).Include(y => y.Questions.OrderBy(z => z.QuestionId))
                                  where request.ProductIds.Contains(p.ProductId)
                                  select p).ToList();
            }

            XLWorkbook workbook = new XLWorkbook();

            int row = 1;

            foreach (var product in targetProducts)
            {
                IXLWorksheet worksheet = workbook.Worksheets.Add(product.Name);

                worksheet.Cell("A" + row).Value = "Продукт";

                worksheet.Range($"A{row}:E{row}").Style.Fill.BackgroundColor = XLColor.DarkGreen;

                worksheet.Range($"A{row}:E{row}").Style.Font.FontColor = XLColor.White;

                worksheet.Range($"A{row}:E{row}").Merge();

                row++;

                worksheet.Cell("A" + row).Value = "ID продукта";
                worksheet.Cell("B" + row).Value = "Название";
                worksheet.Cell("C" + row).Value = "Цвет";
                worksheet.Cell("D" + row).Value = "Логотип";
                worksheet.Cell("E" + row).Value = "Раунд";

                worksheet.Range($"A{row}:E{row}").Style.Fill.BackgroundColor = XLColor.Green;

                worksheet.Range($"A{row}:E{row}").Style.Font.FontColor = XLColor.White;

                row++;

                worksheet.Cell("A" + row).Value = product.ProductId;
                worksheet.Cell("B" + row).Value = product.Name;
                worksheet.Cell("C" + row).Value = product.Colour;
                worksheet.Cell("D" + row).Value = product.Logo;
                worksheet.Cell("E" + row).Value = product.Round;

                worksheet.Range($"A{row}:E{row}").Style.Fill.BackgroundColor = XLColor.LightGreen;

                row++;
                row++;

                worksheet.Cell("A" + row).Value = "Вопросы";

                worksheet.Range($"A{row}:H{row}").Style.Fill.BackgroundColor = XLColor.DarkGreen;

                worksheet.Range($"A{row}:H{row}").Style.Font.FontColor = XLColor.White;

                worksheet.Range($"A{row}:H{row}").Merge();

                row++;

                worksheet.Cell("A" + row).Value = "ID продукта";
                worksheet.Cell("B" + row).Value = "ID вопроса";
                worksheet.Cell("C" + row).Value = "Категория";
                worksheet.Cell("D" + row).Value = "Краткое обозначение";
                worksheet.Cell("E" + row).Value = "Формулировка";
                worksheet.Cell("F" + row).Value = "Варианты ответов";
                worksheet.Cell("G" + row).Value = "Медиа";
                worksheet.Cell("H" + row).Value = "Дата последнего редактирования";

                worksheet.Range($"A{row}:H{row}").Style.Fill.BackgroundColor = XLColor.Green;

                worksheet.Range($"A{row}:H{row}").Style.Font.FontColor = XLColor.White;

                row++;

                QuestionAnswersDTO? questionAnswersDTO;

                foreach (var question in product.Questions)
                {
                    string? answers = question.Answers;

                    try
                    {
                        questionAnswersDTO = JsonSerializer.Deserialize<QuestionAnswersDTO>(answers);

                        if (questionAnswersDTO == null)
                            questionAnswersDTO = new QuestionAnswersDTO();

                        List<string> allAnswers = questionAnswersDTO.AllAnswers.ToList();
                        List<string> correctAnswers = questionAnswersDTO.CorrectAnswers.ToList();

                        for (int i = 0; i < allAnswers.Count; i++)
                        {
                            if (correctAnswers.Contains(allAnswers[i]))
                                allAnswers[i] = allAnswers[i].Insert(0, "[(*)");
                            else
                                allAnswers[i] = allAnswers[i].Insert(0, "[");
                            allAnswers[i] = allAnswers[i].Insert(allAnswers[i].Length, "]");
                        }

                        answers = string.Join(';', allAnswers);
                    }
                    catch { }

                    worksheet.Cell("A" + row).Value = question.ProductId;
                    worksheet.Cell("B" + row).Value = question.QuestionId;
                    worksheet.Cell("C" + row).Value = question.Type;
                    worksheet.Cell("D" + row).Value = question.ShortText;
                    worksheet.Cell("E" + row).Value = question.Text;
                    worksheet.Cell("F" + row).Value = answers;
                    worksheet.Cell("G" + row).Value = question.Media;
                    worksheet.Cell("H" + row).Value = question.LastEditDate;

                    worksheet.Range($"A{row}:H{row}").Style.Fill.BackgroundColor = XLColor.LightGreen;

                    row++;
                }

                if (row > 1)
                {
                    worksheet.Range($"A1:H{row - 1}").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    worksheet.Range($"A1:H{row - 1}").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                    worksheet.Range($"A1:H{row - 1}").Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                    worksheet.Range($"A1:H{row - 1}").Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                }

                row = 1;

                worksheet.Cell("J1").Value = "Справочник категорий вопросов";
                worksheet.Cell("J1").Style.Fill.BackgroundColor = XLColor.DarkGreen;
                worksheet.Cell("J1").Style.Font.FontColor = XLColor.White;

                worksheet.Cell("J2").Style.Fill.BackgroundColor = XLColor.Green;
                worksheet.Cell("J2").Style.Font.FontColor = XLColor.White;

                worksheet.Cell("J3").Value = "TEXT - Без выбора ответа";
                worksheet.Cell("J4").Value = "TEXT_WITH_ANSWERS - С выбором ответа";
                worksheet.Cell("J5").Value = "AUCTION - Вопрос-аукцион";
                worksheet.Cell("J6").Value = "MEDIA - Вопрос с медиа фрагментом";
                worksheet.Range($"J3:J6").Style.Fill.BackgroundColor = XLColor.LightGreen;

                worksheet.Range($"J1:J6").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                worksheet.Range($"J1:J6").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                worksheet.Range($"J1:J6").Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                worksheet.Range($"J1:J6").Style.Border.OutsideBorder = XLBorderStyleValues.Thin;



                worksheet.Cell("J8").Value = "Шаблон вариантов ответа";
                worksheet.Cell("J8").Style.Fill.BackgroundColor = XLColor.DarkGreen;
                worksheet.Cell("J8").Style.Font.FontColor = XLColor.White;

                worksheet.Cell("J9").Style.Fill.BackgroundColor = XLColor.Green;
                worksheet.Cell("J9").Style.Font.FontColor = XLColor.White;

                worksheet.Cell("J10").Value = "[Неверный ответ];[(*)Верный ответ];[Неверный ответ]";
                worksheet.Cell("J10").Style.Fill.BackgroundColor = XLColor.LightGreen;

                worksheet.Range($"J8:J10").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                worksheet.Range($"J8:J10").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                worksheet.Range($"J8:J10").Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                worksheet.Range($"J8:J10").Style.Border.OutsideBorder = XLBorderStyleValues.Thin;



                worksheet.Columns().AdjustToContents();
            }

            List<string> oldFiles = (from file in Directory.GetFiles(_app.Configuration["SourcePath"])
                                     where Regex.IsMatch(Path.GetFileName(file), @"^product.*\.xlsx$")
                                     select file).ToList();

            foreach (var oldFile in oldFiles)
            {
                File.Delete(oldFile);
            }

            string filePath = $"{_app.Configuration["SourcePath"]}{request.FileName}.xlsx";

            using (FileStream fileStream = new FileStream(filePath, FileMode.Create))
            {
                workbook.SaveAs(fileStream);
            }

            return Results.Ok();
        }
        public IResult DeleteProductById(eco_questContext db, long id)
        {
            Product? targetProduct = (from p in db.Products
                                      where p.ProductId == id
                                      select p).FirstOrDefault();

            if (targetProduct != null)
                db.Products.Remove(targetProduct);

            db.SaveChanges();

            return Results.Ok();
        }
        public IResult GetAllProducts(eco_questContext db)
        {
            List<Product> allProducts = db.Products.Include(x => x.Questions).ToList();

            foreach (var product in allProducts)
            {
                foreach (var question in product.Questions)
                    question.Product = null;
            }

            return Results.Json(allProducts.OrderBy(x => x.ProductId));
        }
        public IResult GetAllProductsByRound(eco_questContext db, int round)
        {
            List<Product> targetProducts = db.Products.Where(x => x.Round == round).Include(y => y.Questions).ToList();

            foreach (var product in targetProducts)
            {
                foreach (var question in product.Questions)
                    question.Product = null;
            }

            return Results.Json(targetProducts.OrderBy(x => x.ProductId));
        }
        public IResult ImportProducts(eco_questContext db, HttpRequest request)
        {
            IFormFile? file = request.Form.Files.FirstOrDefault();

            if (file == null)
                return Results.BadRequest("Файл не загружен");

            using (Stream stream = file.OpenReadStream())
            {
                XLWorkbook workbook = new XLWorkbook(stream);

                QuestionAnswersDTO questionAnswersDTO;

                foreach (var worksheet in workbook.Worksheets)
                {
                    long productId1;
                    bool productId1ParsingResult = long.TryParse(worksheet.Cell("A3").Value.ToString(), out productId1);
                    if (!productId1ParsingResult)
                        productId1 = 0;

                    string? name = worksheet.Cell("B3").Value.ToString();
                    string? colour = worksheet.Cell("C3").Value.ToString();

                    int round;
                    bool roundParsingResult = int.TryParse(worksheet.Cell("E3").Value.ToString(), out round);
                    if (!roundParsingResult)
                        round = 0;

                    string? logo = worksheet.Cell("D3").Value.ToString();
                    if (!(Path.GetFileNameWithoutExtension(logo) == $"logo{productId1}" && File.Exists(_app.Configuration["SourcePath"] + logo)))
                        logo = null;

                    Product newProduct = new Product()
                    {
                        ProductId = productId1,
                        Colour = colour,
                        Name = name,
                        Round = round,
                        Logo = logo
                    };

                    int row = 7;

                    bool IsRowEmpty(IXLWorksheet worksheet, int row)
                    {
                        bool isRowEmpty = true;

                        char[] columns = new char[] { 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H' };

                        foreach (var column in columns)
                        {
                            if (!string.IsNullOrEmpty(worksheet.Cell($"{column}{row}").Value.ToString()))
                            {
                                isRowEmpty = false;
                                break;
                            }
                        }

                        return isRowEmpty;
                    }

                    while (!IsRowEmpty(worksheet, row))
                    {
                        long productId2;
                        bool productId2ParsingResult = long.TryParse(worksheet.Cell("A" + row).Value.ToString(), out productId2);
                        if (!productId2ParsingResult)
                            productId2 = 0;

                        long questionId;
                        bool questionIdParsingResult = long.TryParse(worksheet.Cell("B" + row).Value.ToString(), out questionId);
                        if (!questionIdParsingResult)
                            questionId = 0;

                        string? type = worksheet.Cell("C" + row).Value.ToString();
                        string? shortText = worksheet.Cell("D" + row).Value.ToString();
                        string? text = worksheet.Cell("E" + row).Value.ToString();
                        string? answers = worksheet.Cell("F" + row).Value.ToString();
                        string? lastEditDate = worksheet.Cell("H" + row).Value.ToString();

                        string? media = worksheet.Cell("G" + row).Value.ToString();
                        if (!(Path.GetFileNameWithoutExtension(media) == $"media{questionId}" && File.Exists(_app.Configuration["SourcePath"] + media)))
                            media = null;

                        if (answers != null)
                        {
                            questionAnswersDTO = new QuestionAnswersDTO();

                            Regex allAnswersRegex = new Regex(@"\[[^]]+]");
                            Regex correctAnswersRegex = new Regex(@"\[\(\*\)[^]]+]");

                            MatchCollection allAnswersMatches = allAnswersRegex.Matches(answers);
                            MatchCollection correctAnswersMatches = correctAnswersRegex.Matches(answers);

                            questionAnswersDTO.AllAnswers = (from match in allAnswersMatches select match.Value.TrimStart('[').TrimEnd(']').Replace("(*)", "")).ToList();
                            questionAnswersDTO.CorrectAnswers = (from match in correctAnswersMatches select match.Value.TrimStart('[').TrimEnd(']').Replace("(*)", "")).ToList();

                            answers = JsonSerializer.Serialize(questionAnswersDTO);
                        }

                        Question newQuestion = new Question()
                        {
                            QuestionId = questionId,
                            Answers = answers,
                            Type = type,
                            ShortText = shortText,
                            Text = text,
                            ProductId = productId2,
                            Media = media,
                            LastEditDate = lastEditDate
                        };

                        newProduct.Questions.Add(newQuestion);

                        row++;
                    }

                    (bool, IResult) validResult = RequestValidator.ValidateProductModel(db, newProduct);

                    if (!validResult.Item1)
                        continue;

                    Product? targetProduct = (from p in db.Products.Include(x => x.Questions)
                                              where p.ProductId == newProduct.ProductId
                                              select p).FirstOrDefault();

                    if (targetProduct == null)
                    {
                        DeleteProductById(db, newProduct.ProductId);
                        CreateProduct(db, newProduct);
                        continue;
                    }

                    targetProduct.Colour = newProduct.Colour;
                    targetProduct.Name = newProduct.Name;
                    targetProduct.Round = newProduct.Round;
                    targetProduct.Logo = newProduct.Logo;

                    targetProduct.Questions.Clear();

                    foreach (var question in newProduct.Questions)
                    {
                        Question newQuestion = new Question()
                        {
                            Answers = question.Answers,
                            Type = question.Type,
                            ShortText = question.ShortText,
                            Text = question.Text,
                            Media = question.Media,
                            LastEditDate = question.LastEditDate
                        };

                        newQuestion.LastEditDate = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Ekaterinburg Standard Time")).ToString(new CultureInfo("en-US"));
                        if (newQuestion.Type != "MEDIA") newQuestion.Media = null;

                        targetProduct.Questions.Add(newQuestion);
                    }

                    db.SaveChanges();
                }
            }

            return Results.Ok();
        }
        public IResult CreateLogoById(eco_questContext db, HttpRequest request, long id)
        {
            Product? targetProduct = (from p in db.Products
                                      where p.ProductId == id
                                      select p).FirstOrDefault();

            if (targetProduct == null)
                return Results.NotFound("Запрашиваемый продукт не найден");
            if (targetProduct.Logo != null)
                return Results.BadRequest("У запрашиваемого продукта логотип уже существует");

            IFormFile? file = request.Form.Files.FirstOrDefault();

            if (file == null)
                return Results.BadRequest("Файл не загружен");

            string filePath = $"{_app.Configuration["SourcePath"]}logo{id}{Path.GetExtension(file.FileName)}";

            using (FileStream fileStream = new FileStream(filePath, FileMode.Create))
            {
                file.CopyTo(fileStream);
            }

            targetProduct.Logo = Path.GetFileName(filePath);

            db.SaveChanges();

            return Results.Ok();
        }
        public IResult DeleteLogoById(eco_questContext db, long id)
        {
            Product? targetProduct = (from p in db.Products
                                      where p.ProductId == id
                                      select p).FirstOrDefault();

            if (targetProduct != null && targetProduct.Logo != null)
            {
                FileInfo? targetFile = (from f in new DirectoryInfo(_app.Configuration["SourcePath"]).GetFiles()
                                        where f.Name == Path.GetFileName(targetProduct.Logo)
                                        select f).FirstOrDefault();

                if (targetFile != null)
                    targetFile.Delete();

                targetProduct.Logo = null;
            }

            db.SaveChanges();

            return Results.Ok();
        }
        public IResult UpdateLogoById(eco_questContext db, HttpRequest request, long id)
        {
            DeleteLogoById(db, id);
            return CreateLogoById(db, request, id);
        }
        public IResult UpdateProduct(eco_questContext db, Product request)
        {
            (bool, IResult) validResult = RequestValidator.ValidateProductModel(db, request);

            if (!validResult.Item1)
                return validResult.Item2;

            Product? targetProduct = (from p in db.Products.Include(x => x.Questions)
                                      where p.ProductId == request.ProductId
                                      select p).FirstOrDefault();

            if (targetProduct == null)
            {
                DeleteProductById(db, request.ProductId);
                return CreateProduct(db, request);
            }

            targetProduct.Colour = request.Colour;
            targetProduct.Name = request.Name;
            targetProduct.Round = request.Round;
            targetProduct.Logo = request.Logo;

            foreach (var question in request.Questions)
            {
                Question? targetQuestion = (from q in targetProduct.Questions
                                            where q.QuestionId == question.QuestionId
                                            select q).FirstOrDefault();

                if (targetQuestion == null)
                {
                    Question newQuestion = new Question()
                    {
                        Answers = question.Answers,
                        Type = question.Type,
                        ShortText = question.ShortText,
                        Text = question.Text,
                        Media = question.Media,
                        LastEditDate = question.LastEditDate
                    };

                    newQuestion.LastEditDate = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Ekaterinburg Standard Time")).ToString(new CultureInfo("en-US"));
                    if (newQuestion.Type != "MEDIA") newQuestion.Media = null;

                    targetProduct.Questions.Add(newQuestion);
                }
                else
                {
                    targetQuestion.Answers = question.Answers;
                    targetQuestion.Type = question.Type;
                    targetQuestion.ShortText = question.ShortText;
                    targetQuestion.Text = question.Text;
                    targetQuestion.LastEditDate = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Ekaterinburg Standard Time")).ToString(new CultureInfo("en-US"));

                    if (targetQuestion.Type != "MEDIA") targetQuestion.Media = null;
                    else targetQuestion.Media = question.Media;
                }
            }

            db.SaveChanges();

            return Results.Ok();
        }

        public IResult DeleteQuestionById(eco_questContext db, long id)
        {
            Question? targetQuestion = (from q in db.Questions
                                        where q.QuestionId == id
                                        select q).FirstOrDefault();

            if (targetQuestion != null)
                db.Questions.Remove(targetQuestion);

            db.SaveChanges();

            return Results.Ok();
        }
        public IResult CreateMediaById(eco_questContext db, HttpRequest request, long id)
        {
            Question? targetQuestion = (from q in db.Questions
                                        where q.QuestionId == id
                                        select q).FirstOrDefault();

            if (targetQuestion == null)
                return Results.NotFound("Запрашиваемый вопрос не найден");
            if (targetQuestion.Type != "MEDIA")
                return Results.BadRequest("Тип запрашиваемого вопроса не является 'медиа'");
            if (targetQuestion.Media != null)
                return Results.BadRequest("У запрашиваемого вопроса медиа уже существует");

            IFormFile? file = request.Form.Files.FirstOrDefault();

            if (file == null)
                return Results.BadRequest("Файл не загружен");

            string filePath = $"{_app.Configuration["SourcePath"]}media{id}{Path.GetExtension(file.FileName)}";

            using (FileStream fileStream = new FileStream(filePath, FileMode.Create))
            {
                file.CopyTo(fileStream);
            }

            targetQuestion.Media = Path.GetFileName(filePath);

            db.SaveChanges();

            return Results.Ok();
        }
        public IResult DeleteMediaById(eco_questContext db, long id)
        {
            Question? targetQuestion = (from q in db.Questions
                                        where q.QuestionId == id
                                        select q).FirstOrDefault();

            if (targetQuestion != null && targetQuestion.Media != null)
            {
                FileInfo? targetFile = (from f in new DirectoryInfo(_app.Configuration["SourcePath"]).GetFiles()
                                        where f.Name == Path.GetFileName(targetQuestion.Media)
                                        select f).FirstOrDefault();

                if (targetFile != null)
                    targetFile.Delete();

                targetQuestion.Media = null;
            }

            db.SaveChanges();

            return Results.Ok();
        }
        public IResult UpdateMediaById(eco_questContext db, HttpRequest request, long id)
        {
            DeleteMediaById(db, id);
            return CreateMediaById(db, request, id);
        }

        public IResult CreateStatistic(eco_questContext db, Statistic request)
        {
            (bool, IResult) validResult = RequestValidator.ValidateStatisticModel(db, request);

            if (!validResult.Item1)
                return validResult.Item2;

            Statistic newRecord = new Statistic()
            {
                UserId = request.UserId,
                LastName = request.LastName,
                FirstName = request.FirstName,
                Patronymic = request.Patronymic,
                Login = request.Login,
                Date = request.Date,
                Duration = request.Duration,
                Results = request.Results
            };

            db.Statistics.Add(newRecord);

            db.SaveChanges();

            return Results.Ok();
        }
        public IResult ExportStatistic(eco_questContext db, StatisticExportDTO request)
        {
            if (request.FileName == null || request.FileName == string.Empty)
                return Results.BadRequest("Имя файла не может иметь значение null");

            List<Statistic> allRecords = db.Statistics.ToList();
            List<(Statistic, DateTime, TimeSpan)> allRecordsDatesDurations = new List<(Statistic, DateTime, TimeSpan)>();

            foreach (var record in allRecords)
                allRecordsDatesDurations.Add((record, DateTime.Parse(record.Date, new CultureInfo("en-US"), DateTimeStyles.None), TimeSpan.Parse(record.Duration, new CultureInfo("en-US"))));

            DateTime startDate;
            DateTime endDate;
            TimeSpan startDuration;
            TimeSpan endDuration;

            bool startDateParsingResult = DateTime.TryParse(request.StartDate, new CultureInfo("en-US"), DateTimeStyles.None, out startDate);
            bool endDateParsingResult = DateTime.TryParse(request.EndDate, new CultureInfo("en-US"), DateTimeStyles.None, out endDate);
            bool startDurationParsingResult = TimeSpan.TryParse(request.StartDuration, new CultureInfo("en-US"), out startDuration);
            bool endDurationParsingResult = TimeSpan.TryParse(request.EndDuration, new CultureInfo("en-US"), out endDuration);

            if (!startDateParsingResult)
                startDate = allRecordsDatesDurations.Min(x => x.Item2);
            if (!endDateParsingResult)
                endDate = allRecordsDatesDurations.Max(x => x.Item2);
            if (!startDurationParsingResult)
                startDuration = allRecordsDatesDurations.Min(x => x.Item3);
            if (!endDurationParsingResult)
                endDuration = allRecordsDatesDurations.Max(x => x.Item3);

            if (!(startDate <= endDate))
                return Results.BadRequest("Дата начала не может быть больше даты конца");
            if (!(startDuration <= endDuration))
                return Results.BadRequest("Продолжительность начала не может быть больше продолжительности конца");

            List<Statistic> targetRecords = (from r in allRecordsDatesDurations
                                             where r.Item2 >= startDate && r.Item2 <= endDate && r.Item3 >= startDuration && r.Item3 <= endDuration
                                             orderby r.Item2, r.Item3
                                             select r.Item1).ToList();

            XLWorkbook workbook = new XLWorkbook();
            IXLWorksheet worksheet = workbook.Worksheets.Add("Statistics");

            int row = 1;

            worksheet.Cell("B" + row).Value = "Дата проведения игры";
            worksheet.Cell("C" + row).Value = "Продолжительность игры";
            worksheet.Cell("D" + row).Value = "ФИО ведущего";
            worksheet.Cell("E" + row).Value = "Логин ведущего";
            worksheet.Cell("F" + row).Value = "Команда";
            worksheet.Cell("G" + row).Value = "Игрок";
            worksheet.Cell("H" + row).Value = "Очки";
            worksheet.Cell("I" + row).Value = "Место";

            worksheet.Range($"B{row}:I{row}").Style.Fill.BackgroundColor = XLColor.DarkGreen;

            worksheet.Range($"B{row}:I{row}").Style.Font.FontColor = XLColor.White;

            row++;

            StatisticsResultsDTO? statisticsResultsDTO;

            foreach (var record in targetRecords)
            {
                statisticsResultsDTO = JsonSerializer.Deserialize<StatisticsResultsDTO>(record.Results);
                if (statisticsResultsDTO == null)
                    statisticsResultsDTO = new StatisticsResultsDTO();

                foreach (var team in statisticsResultsDTO.Teams)
                {
                    foreach (var player in team.Players)
                    {
                        worksheet.Cell("A" + row).Value = record.RecordId;
                        worksheet.Cell("B" + row).Value = record.Date;
                        worksheet.Cell("C" + row).Value = record.Duration;
                        worksheet.Cell("D" + row).Value = $"{record.LastName} {record.FirstName} {record.Patronymic}";
                        worksheet.Cell("E" + row).Value = record.Login;
                        worksheet.Cell("F" + row).Value = team.Name;
                        worksheet.Cell("G" + row).Value = player;
                        worksheet.Cell("H" + row).Value = team.Score;
                        worksheet.Cell("I" + row).Value = team.Place;

                        row++;
                    }
                }
            }

            if (targetRecords.Count > 0)
            {
                worksheet.Range($"A2:A{row - 1}").Style.Fill.BackgroundColor = XLColor.LightGreen;
                worksheet.Range($"B2:I{row - 1}").Style.Fill.BackgroundColor = XLColor.Green;

                worksheet.Range($"B2:I{row - 1}").Style.Font.FontColor = XLColor.White;
            }

            worksheet.Columns().AdjustToContents();

            worksheet.Range($"A1:I{row - 1}").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Range($"A1:I{row - 1}").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

            worksheet.Range($"A1:I{row - 1}").Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            worksheet.Range($"A1:I{row - 1}").Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

            List<string> oldFiles = (from file in Directory.GetFiles(_app.Configuration["SourcePath"])
                                     where Regex.IsMatch(Path.GetFileName(file), @"^statistics.*\.xlsx$")
                                     select file).ToList();

            foreach (var oldFile in oldFiles)
            {
                File.Delete(oldFile);
            }

            string filePath = $"{_app.Configuration["SourcePath"]}{request.FileName}.xlsx";

            using (FileStream fileStream = new FileStream(filePath, FileMode.Create))
            {
                workbook.SaveAs(fileStream);
            }

            return Results.Ok();
        }

        public IResult CreateUser(eco_questContext db, User request)
        {
            (bool, IResult) validResult = RequestValidator.ValidateUserModel(db, request);

            if (!validResult.Item1)
                return validResult.Item2;

            User newUser = new User()
            {
                LastName = request.LastName,
                FirstName = request.FirstName,
                Patronymic = request.Patronymic,
                Login = request.Login,
                Password = PasswordHasher.Encrypt(request.Password),
                Role = "master",
                Status = "inactive"
            };

            db.Users.Add(newUser);

            db.SaveChanges();

            return Results.Ok();
        }
        public IResult DeleteUserById(eco_questContext db, long id)
        {
            DeleteExpiredGames(db);

            User? targetUser = (from u in db.Users
                                where u.UserId == id
                                select u).FirstOrDefault();

            if (targetUser != null)
                db.Users.Remove(targetUser);

            db.SaveChanges();

            return Results.Ok();
        }
        public IResult GetActiveMasters(eco_questContext db)
        {
            List<User> targetUsers = (from u in db.Users
                                      where u.Role == "master" && u.Status == "active"
                                      select u).ToList();

            foreach (var user in targetUsers)
            {
                user.Password = null;
                user.Role = null;
                user.Status = null;
            }

            return Results.Json(targetUsers.OrderBy(x => x.UserId));
        }
        public IResult GetInactiveMasters(eco_questContext db)
        {
            List<User> targetUsers = (from u in db.Users
                                      where u.Role == "master" && u.Status == "inactive"
                                      select u).ToList();

            foreach (var user in targetUsers)
            {
                user.Password = null;
                user.Role = null;
                user.Status = null;
            }

            return Results.Json(targetUsers.OrderBy(x => x.UserId));
        }
        public IResult ToActiveMasterById(eco_questContext db, long id)
        {
            User? targetUser = (from u in db.Users
                                where u.UserId == id
                                select u).FirstOrDefault();

            if (targetUser == null)
                return Results.NotFound("Запрашиваемый пользователь не найден");
            if (targetUser.Role != "master")
                return Results.BadRequest("Запрашиваемый пользователь не является ведущим");
            if (targetUser.Status != "inactive")
                return Results.BadRequest("Запрашиваемый пользователь не является неактивным");

            targetUser.Status = "active";

            db.SaveChanges();

            return Results.Ok();
        }
        public IResult ToInactiveMasterById(eco_questContext db, long id)
        {
            User? targetUser = (from u in db.Users
                                where u.UserId == id
                                select u).FirstOrDefault();

            if (targetUser == null)
                return Results.NotFound("Запрашиваемый пользователь не найден");
            if (targetUser.Role != "master")
                return Results.BadRequest("Запрашиваемый пользователь не является ведущим");
            if (targetUser.Status != "active")
                return Results.BadRequest("Запрашиваемый пользователь не является активным");

            targetUser.Status = "inactive";

            db.SaveChanges();

            return Results.Ok();
        }
        public IResult UpdateUserInfo(eco_questContext db, User request)
        {
            (bool, IResult) validResult = RequestValidator.ValidateUserModel(db, request);

            if (!validResult.Item1)
                return validResult.Item2;

            User? targetUser = (from u in db.Users
                                where u.UserId == request.UserId
                                select u).FirstOrDefault();

            if (targetUser == null)
                return Results.NotFound("Запрашиваемый пользователь не найден");
            if (targetUser.Password != PasswordHasher.Encrypt(request.Password))
                return Results.BadRequest("Запрашиваемый пароль пользователя не совпадает с фактическим");

            targetUser.LastName = request.LastName;
            targetUser.FirstName = request.FirstName;
            targetUser.Patronymic = request.Patronymic;
            targetUser.Login = request.Login;

            db.SaveChanges();

            return Results.Ok();
        }
        public IResult UpdateUserPassword(eco_questContext db, UpdatePasswordDTO request)
        {
            (bool, IResult) validResult = RequestValidator.ValidateUpdatePasswordDTO(db, request);

            if (!validResult.Item1)
                return validResult.Item2;

            User? targetUser = (from u in db.Users
                                where u.Login == request.Login
                                select u).FirstOrDefault();

            if (targetUser == null)
                return Results.NotFound("Запрашиваемый пользователь не найден");
            if (targetUser.Password != PasswordHasher.Encrypt(request.OldPassword))
                return Results.BadRequest("Запрашиваемый пароль пользователя не совпадает с фактическим");

            targetUser.Password = PasswordHasher.Encrypt(request.NewPassword);

            db.SaveChanges();

            return Results.Ok();
        }
        public IResult ResetUserPassword(eco_questContext db, UpdatePasswordDTO request)
        {
            if (request.Login == null || request.Login == string.Empty)
                return Results.BadRequest("Логин пользователя не может иметь значение null");
            if (request.NewPassword == null || request.NewPassword == string.Empty)
                return Results.BadRequest("Новый пароль пользователя не может иметь значение null");

            User? targetUser = (from u in db.Users
                                where u.Login == request.Login
                                select u).FirstOrDefault();

            if (targetUser == null)
                return Results.NotFound("Запрашиваемый пользователь не найден");

            targetUser.Password = PasswordHasher.Encrypt(request.NewPassword);

            db.SaveChanges();

            return Results.Ok();
        }

        public void DeleteExpiredGames(eco_questContext db)
        {
            List<Game> allGames = db.Games.ToList();
            List<(Game, DateTime)> allGamesDates = new List<(Game, DateTime)>();

            foreach (var game in allGames)
                allGamesDates.Add((game, DateTime.Parse(game.Date, new CultureInfo("en-US"), DateTimeStyles.None)));

            List<Game> targetGames = (from g in allGamesDates
                                      where DateTime.UtcNow >= g.Item2.Add(TimeSpan.FromDays(7))
                                      select g.Item1).ToList();

            db.Games.RemoveRange(targetGames);

            db.SaveChanges();
        }
    }
}