# ---- build stage ----
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Couppa.sln .
COPY Couppa.Api/Couppa.Api.csproj Couppa.Api/
COPY Couppa.Api.Tests/Couppa.Api.Tests.csproj Couppa.Api.Tests/
RUN dotnet restore Couppa.Api/Couppa.Api.csproj

COPY Couppa.Api/ Couppa.Api/

# Chưa có migration nào được commit sẵn (xem README) -> tự generate InitialCreate ở build time
# nếu thư mục Data/Migrations rỗng, để Program.cs (Database.MigrateAsync) áp dụng khi container start.
RUN dotnet tool install --global dotnet-ef --version 8.* \
    && export PATH="$PATH:/root/.dotnet/tools" \
    && if [ -z "$(ls -A Couppa.Api/Data/Migrations 2>/dev/null)" ]; then \
         dotnet ef migrations add InitialCreate \
           --project Couppa.Api/Couppa.Api.csproj \
           --startup-project Couppa.Api/Couppa.Api.csproj \
           --output-dir Data/Migrations; \
       fi

RUN dotnet publish Couppa.Api/Couppa.Api.csproj -c Release -o /app/publish --no-restore

# ---- runtime stage ----
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
COPY --from=build /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Docker
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Couppa.Api.dll"]
