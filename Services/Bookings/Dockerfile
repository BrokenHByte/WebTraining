FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["Contracts/Contracts.csproj", "Contracts/"]
COPY ["Bookings/Domain/Bookings.Domain.csproj", "Bookings/Domain/"]
COPY ["Bookings/Application/Bookings.Application.csproj", "Bookings/Application/"]
COPY ["Bookings/Infrastructure/Bookings.Infrastructure.csproj", "Bookings/Infrastructure/"]
COPY ["Bookings/Presentation/Bookings.Presentation.csproj", "Bookings/Presentation/"]

RUN dotnet restore "Bookings/Presentation/Bookings.Presentation.csproj"

COPY ["Contracts/", "Contracts/"]
COPY ["Bookings/Domain/", "Bookings/Domain/"]
COPY ["Bookings/Application/", "Bookings/Application/"]
COPY ["Bookings/Infrastructure/", "Bookings/Infrastructure/"]
COPY ["Bookings/Presentation/", "Bookings/Presentation/"]

WORKDIR /src/Bookings/Presentation
RUN dotnet publish "Bookings.Presentation.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

RUN apt-get update \
    && apt-get install -y --no-install-recommends libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/*
	
COPY --from=build /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true
	
EXPOSE 8080

ENTRYPOINT ["dotnet", "Bookings.Presentation.dll"]