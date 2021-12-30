
FROM mcr.microsoft.com/dotnet/runtime:5.0 AS base
LABEL maintainer="Anders Forsgren <anders.forsgren@gmail.com>"
LABEL version="1.0.0"
LABEL description="owhttp to mqtt relay"

WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src
COPY ["owfsmq.csproj", "."]
RUN dotnet restore "./owfsmq.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "owfsmq.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "owfsmq.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "owfsmq.dll"]
