using EcoQuest.Models;
using EcoQuest.Models.DTO;
using EcoQuest.Models.Entities;

namespace EcoQuest.Services
{
    public static class ModelConverter
    {
        public static GameBoard ToGameBoard(eco_questContext db, GameBoardDTO gameBoardDTO)
        {
            GameBoard gameBoard = new GameBoard()
            {
                GameBoardId = gameBoardDTO.GameBoardId,
                Name = gameBoardDTO.Name,
                NumFields = gameBoardDTO.NumFields,
                UserId = gameBoardDTO.UserId
            };

            foreach (var product in gameBoardDTO.Products)
            {
                GameBoardsProduct gameBoardsProduct = new GameBoardsProduct()
                {
                    GameBoardId = product.GameBoardId,
                    ProductId = product.ProductId,
                    NumOfRepeating = product.NumOfRepeating,
                    Product = new Product()
                    {
                        ProductId = product.ProductId,
                        Colour = product.Colour,
                        Name = product.Name,
                        Round = product.Round,
                        Logo = product.Logo
                    }
                };

                List<Question> questions = (from q in db.Questions
                                            where product.ActiveQuestions.Contains(q.QuestionId)
                                            select q).ToList();

                gameBoard.GameBoardsProducts.Add(gameBoardsProduct);

                foreach (var question in questions)
                    gameBoard.Questions.Add(question);
            }

            return gameBoard;
        }
        public static GameBoardDTO ToGameBoardDTO(eco_questContext db, GameBoard gameBoard)
        {
            GameBoardDTO gameBoardDTO = new GameBoardDTO()
            {
                GameBoardId = gameBoard.GameBoardId,
                Name = gameBoard.Name,
                NumFields = gameBoard.NumFields,
                UserId = gameBoard.UserId
            };

            foreach (var gameBoardsProduct in gameBoard.GameBoardsProducts)
            {
                ProductDTO productDTO = new ProductDTO()
                {
                    GameBoardId = gameBoardsProduct.GameBoardId,
                    ProductId = gameBoardsProduct.ProductId,
                    Colour = gameBoardsProduct.Product.Colour,
                    Name = gameBoardsProduct.Product.Name,
                    Round = gameBoardsProduct.Product.Round,
                    Logo = gameBoardsProduct.Product.Logo,
                    NumOfRepeating = gameBoardsProduct.NumOfRepeating
                };

                List<Question> allQuestions = (from q in db.Questions
                                               where q.ProductId == gameBoardsProduct.ProductId
                                               select q).ToList();

                List<Question> activeQuestions = (from q in gameBoard.Questions
                                                  where q.ProductId == gameBoardsProduct.ProductId
                                                  select q).ToList();

                foreach (var question in allQuestions)
                {
                    question.Product = null;
                    question.GameBoards = null;

                    productDTO.AllQuestions.Add(question);
                }

                foreach (var question in activeQuestions)
                    productDTO.ActiveQuestions.Add(question.QuestionId);

                gameBoardDTO.Products.Add(productDTO);
            }

            return gameBoardDTO;
        }
    }
}