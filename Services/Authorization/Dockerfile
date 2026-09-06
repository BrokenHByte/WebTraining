FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["Core/Core.csproj", "Core/"]
COPY ["Authorization/Domain/Authorization.Domain.csproj", "Authorization/Domain/"]
COPY ["Authorization/Application/Authorization.Application.csproj", "Authorization/Application/"]
COPY ["Authorization/Infrastructure/Authorization.Infrastructure.csproj", "Authorization/Infrastructure/"]
COPY ["Authorization/Presentation/Authorization.Presentation.csproj", "Authorization/Presentation/"]

RUN dotnet restore "Authorization/Presentation/Authorization.Presentation.csproj"

COPY ["Core/", "Core/"]
COPY ["Authorization/Domain/", "Authorization/Domain/"]
COPY ["Authorization/Application/", "Authorization/Application/"]
COPY ["Authorization/Infrastructure/", "Authorization/Infrastructure/"]
COPY ["Authorization/Presentation/", "Authorization/Presentation/"]

WORKDIR /src/Authorization/Presentation
RUN dotnet publish "Authorization.Presentation.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true
	
EXPOSE 8080

ENTRYPOINT ["dotnet", "Authorization.Presentation.dll"]