using EcoQuest.Services;
using Microsoft.AspNetCore.Authorization;

namespace EcoQuest.Controllers
{
    public class ApplicationController
    {
        public ApplicationController(WebApplication app, ApplicationService service)
        {
            _app = app;
            _service = service;
        }

        private readonly WebApplication _app;
        private readonly ApplicationService _service;

        public void Map()
        {
            _app.MapGet("/", [Authorize(Roles = "adminactive, masteractive, masterinactive, player")] () => Results.Ok());
            _app.MapGet("/auth/login", [Authorize(Roles = "adminactive, masteractive, masterinactive, player")] () => Results.Ok());
            _app.MapGet("/auth/registration", [Authorize(Roles = "adminactive, masteractive, masterinactive, player")] () => Results.Ok());
            _app.MapGet("/fields", [Authorize(Roles = "adminactive")] () => Results.Ok());
            _app.MapGet("/game", [Authorize(Roles = "adminactive, masteractive, player")] () => Results.Ok());
            _app.MapGet("/lobby", [Authorize(Roles = "adminactive, masteractive, player")] () => Results.Ok());
            _app.MapGet("/status", [Authorize(Roles = "adminactive, masteractive")] () => Results.Ok());
            _app.MapGet("/templates", [Authorize(Roles = "adminactive, masteractive")] () => Results.Ok());

            _app.MapPost("/authentication/login/master", _service.LoginMaster);
            _app.MapPost("/authentication/login/player", _service.LoginPlayer);

            _app.MapPost("/game/create", _service.CreateGame);
            _app.MapDelete("/game/delete/{id:long}", _service.DeleteGameById);
            _app.MapGet("/game/get/{id:long}", _service.GetGameById);
            _app.MapGet("/game/get/all", _service.GetAllGames);
            _app.MapGet("/game/get/all/{id:long}", _service.GetAllGamesByUserId);
            _app.MapPost("/game/state/players/create/{id:long}", _service.CreatePlayerById);
            _app.MapDelete("/game/state/players/delete/{gameId:long}/{playerId:long}", _service.DeletePlayerByGameIdAndPlayerId);
            _app.MapPost("/game/state/players/update/{id:long}", _service.UpdatePlayerById);
            _app.MapPost("/game/update", _service.UpdateGame);
            _app.MapPost("/game/update/stateAndQuestion", _service.UpdateStateAndQuestion);

            _app.MapPost("/gameBoard/create", _service.CreateGameBoard);
            _app.MapDelete("/gameBoard/delete/{id:long}", _service.DeleteGameBoardById);
            _app.MapGet("/gameBoard/get/{id:long}", _service.GetGameBoardById);
            _app.MapGet("/gameBoard/get/all", _service.GetAllGameBoards);
            _app.MapGet("/gameBoard/get/all/{id:long}", _service.GetAllGameBoardsByUserId);
            _app.MapPost("/gameBoard/share/{fromUserId:long}/{gameBoardId:long}/{toUserId:long}", _service.ShareGameBoard);
            _app.MapPost("/gameBoard/update", _service.UpdateGameBoard);

            _app.MapPost("/product/create", _service.CreateProduct);
            _app.MapPost("/product/export", _service.ExportProducts);
            _app.MapDelete("/product/delete/{id:long}", _service.DeleteProductById);
            _app.MapGet("/product/get/all", _service.GetAllProducts);
            _app.MapGet("/product/get/all/{round:int}", _service.GetAllProductsByRound);
            _app.MapPost("/product/import", _service.ImportProducts);
            _app.MapPost("/product/logo/create/{id:long}", _service.CreateLogoById);
            _app.MapDelete("/product/logo/delete/{id:long}", _service.DeleteLogoById);
            _app.MapPost("/product/logo/update/{id:long}", _service.UpdateLogoById);
            _app.MapPost("/product/update", _service.UpdateProduct);

            _app.MapDelete("/question/delete/{id:long}", _service.DeleteQuestionById);
            _app.MapPost("/question/media/create/{id:long}", _service.CreateMediaById);
            _app.MapDelete("/question/media/delete/{id:long}", _service.DeleteMediaById);
            _app.MapPost("/question/media/update/{id:long}", _service.UpdateMediaById);

            _app.MapPost("/statistic/create", _service.CreateStatistic);
            _app.MapPost("/statistic/export", _service.ExportStatistic);

            _app.MapPost("/user/create", _service.CreateUser);
            _app.MapDelete("/user/delete/{id:long}", _service.DeleteUserById);
            _app.MapGet("/user/get/activeMasters", _service.GetActiveMasters);
            _app.MapGet("/user/get/inactiveMasters", _service.GetInactiveMasters);
            _app.MapPost("/user/toActiveMaster/{id:long}", _service.ToActiveMasterById);
            _app.MapPost("/user/toInactiveMaster/{id:long}", _service.ToInactiveMasterById);
            _app.MapPost("/user/update/info", _service.UpdateUserInfo);
            _app.MapPost("/user/update/password", _service.UpdateUserPassword);
            _app.MapPost("/user/update/password/reset", _service.ResetUserPassword);
        }
    }
}