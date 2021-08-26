ARG DOTNET_VERSION=6.0
ARG SDK_IMAGE=alpine3.13

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION}-${SDK_IMAGE} AS build-base
ARG RUNTIME_ID=alpine
ENV RUNTIME=${RUNTIME_ID}

FROM build-base AS build-base-amd64
ENV RUNTIME=${RUNTIME}-x64

FROM build-base AS build-base-arm64
ENV RUNTIME=${RUNTIME}-arm64

FROM build-base-${TARGETARCH} AS build-env
RUN mkdir -p /app
COPY ./Directory.Build.props /app
COPY ./src /app/src
RUN dotnet publish \
    --configuration Release \
    --output /dist \
    --self-contained \
    --runtime ${RUNTIME} \
    /app/src/UrlShortener

FROM --platform=$TARGETPLATFORM mcr.microsoft.com/dotnet/runtime-deps:${DOTNET_VERSION}-alpine
COPY --from=build-env /dist /app
WORKDIR /app

ENTRYPOINT [ "/app/UrlShortener" ]
