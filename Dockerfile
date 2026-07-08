FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine3.24 AS build
ENV DOTNET_PRINT_TELEMETRY_MESSAGE=false

WORKDIR /source
COPY src/Inkluzitron/*.csproj src/Inkluzitron/
RUN dotnet restore src/Inkluzitron/ -r linux-musl-x64
COPY . .
RUN dotnet publish src/Inkluzitron/ -c release -o /app -r linux-musl-x64 --no-self-contained --no-restore

FROM mcr.microsoft.com/dotnet/runtime:10.0-alpine3.24
RUN apk add tzdata fontconfig font-opensans
ENV TZ=Europe/Prague
WORKDIR /app
COPY --from=build /app .
RUN ln -snf /usr/share/zoneinfo/$TZ /etc/localtime && echo $TZ > /etc/timezone

ENTRYPOINT ["./Inkluzitron"]
