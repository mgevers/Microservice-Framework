using Ardalis.Result;
using MediatR;

namespace TestApp.Core.Boundary;

public record AddCharacterCommand(Guid CharacterId, string Name) : ICommand { }

public record AddCharacterRequest(Guid CharacterId, string Name) : IRequest<Result> { }

public record RemoveCharacterCommand(Guid CharacterId) : ICommand { }

public record RemoveCharacterRequest(Guid CharacterId) : IRequest<Result> { }

public record UpdateCharacterCommand(Guid CharacterId, string Name) : ICommand { }

public record UpdateCharacterRequest(Guid CharacterId, string Name) : IRequest<Result> { }
