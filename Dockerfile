FROM mcr.microsoft.com/dotnet/sdk:10.0-resolute AS build
ENV DOTNET_PRINT_TELEMETRY_MESSAGE=false

WORKDIR /source
COPY src/Inkluzitron/*.csproj src/Inkluzitron/
RUN dotnet restore src/Inkluzitron/ -r linux-x64
COPY . .
RUN dotnet publish src/Inkluzitron/ -c release -o /app -r linux-x64 --no-self-contained --no-restore

FROM mcr.microsoft.com/dotnet/runtime:10.0-resolute
RUN sed -i'.bak' 's/$/ contrib/' /etc/apt/sources.list
RUN apt-get update && \
    apt-get -y install tzdata fontconfig fonts-open-sans fonts-symbola && \
    apt-get clean
ENV TZ=Europe/Prague
ENV FONTCONFIG_PATH=/etc/fonts
WORKDIR /app
COPY --from=build /app .
RUN ln -snf /usr/share/zoneinfo/$TZ /etc/localtime && echo $TZ > /etc/timezone

STOPSIGNAL SIGINT
ENTRYPOINT ["/app/Inkluzitron"]
