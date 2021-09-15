ARG DOTNET_VERSION=6.0
ARG ALPINE_VERSION=3.13

FROM --platform=arm64 alpine:${ALPINE_VERSION} AS alpine-sqlite-libs
RUN apk add sqlite-libs

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION}-alpine${ALPINE_VERSION} AS build-env
RUN mkdir -p /app
COPY ./Directory.Build.props /app
COPY ./src /app/src
RUN dotnet publish \
    --configuration Release \
    --output /dist \
    /app/src/UrlShortener
RUN mv /dist/runtimes /esql
COPY --from=alpine-sqlite-libs /usr/lib/libsqlite3.so.0 /esql/alpine-arm64/native/libe_sqlite3.so
RUN rm -rf /dist/runtimes && \
    mkdir -p /dist/runtimes && \
    cp -r /esql/alpine-x64 /dist/runtimes && \
    cp -r /esql/alpine-arm64 /dist/runtimes && \
    rm /dist/UrlShortener

FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION}-alpine AS run-base-arm64
ENV RID=alpine-arm64
FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION}-alpine AS run-base-amd64
ENV RID=alpine-x64

FROM --platform=$TARGETPLATFORM run-base-${TARGETARCH}
COPY --from=build-env /dist /app
RUN cp /app/runtimes/${RID}/native/libe_sqlite3.so /app/libe_sqlite3.so
WORKDIR /app

ENTRYPOINT [ "dotnet", "/app/UrlShortener.dll" ]
