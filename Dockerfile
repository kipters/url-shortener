ARG DOTNET_VERSION=7.0
ARG ALPINE_VERSION=3.16

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION}-alpine${ALPINE_VERSION} AS build-env
ARG COMMIT_HASH=dirty
RUN mkdir -p /app
COPY ./Directory.Build.props /app
COPY ./src /app/src
RUN dotnet publish \
    --configuration Release \
    --output /dist \
    -p:SourceRevisionId=${COMMIT_HASH} \
    /app/src/UrlShortener

FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION}-alpine AS run-base-arm64
ENV RID=alpine-arm64
FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION}-alpine AS run-base-amd64
ENV RID=alpine-x64

FROM --platform=$TARGETPLATFORM run-base-${TARGETARCH}
COPY --from=build-env /dist /app
RUN cp /app/runtimes/${RID}/native/libe_sqlite3.so /app/libe_sqlite3.so && rm -rf /app/runtimes
WORKDIR /app

ENTRYPOINT [ "dotnet", "/app/UrlShortener.dll" ]
