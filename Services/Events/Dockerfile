FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["Core/Core.csproj", "Core/"]
COPY ["Events/Domain/Events.Domain.csproj", "Events/Domain/"]
COPY ["Events/Application/Events.Application.csproj", "Events/Application/"]
COPY ["Events/Infrastructure/Events.Infrastructure.csproj", "Events/Infrastructure/"]
COPY ["Events/Presentation/Events.Presentation.csproj", "Events/Presentation/"]

RUN dotnet restore "Events/Presentation/Events.Presentation.csproj"

COPY ["Core/", "Core/"]
COPY ["Events/Domain/", "Events/Domain/"]
COPY ["Events/Application/", "Events/Application/"]
COPY ["Events/Infrastructure/", "Events/Infrastructure/"]
COPY ["Events/Presentation/", "Events/Presentation/"]

WORKDIR /src/Events/Presentation
RUN dotnet publish "Events.Presentation.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

RUN apt-get update \
    && apt-get install -y --no-install-recommends libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true
	
EXPOSE 8080

ENTRYPOINT ["dotnet", "Events.Presentation.dll"]