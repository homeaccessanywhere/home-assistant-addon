# Multi-arch Dockerfile for Home Assistant Addon
ARG BUILD_FROM
FROM ${BUILD_FROM}

# Install .NET runtime
RUN apk add --no-cache \
    bash \
    icu-libs \
    krb5-libs \
    libgcc \
    libintl \
    libssl3 \
    libstdc++ \
    zlib

# Download and install .NET Runtime
ARG DOTNET_VERSION=8.0.11
ARG TARGETARCH
RUN if [ "$TARGETARCH" = "amd64" ]; then \
        DOTNET_ARCH="x64"; \
    elif [ "$TARGETARCH" = "arm64" ]; then \
        DOTNET_ARCH="arm64"; \
    elif [ "$TARGETARCH" = "arm" ]; then \
        DOTNET_ARCH="arm"; \
    fi && \
    wget -O dotnet.tar.gz "https://dotnetcli.azureedge.net/dotnet/Runtime/${DOTNET_VERSION}/dotnet-runtime-${DOTNET_VERSION}-linux-musl-${DOTNET_ARCH}.tar.gz" && \
    mkdir -p /usr/share/dotnet && \
    tar -oxzf dotnet.tar.gz -C /usr/share/dotnet && \
    rm dotnet.tar.gz && \
    ln -s /usr/share/dotnet/dotnet /usr/bin/dotnet

WORKDIR /app

# Copy pre-built application
COPY rootfs/ /

# Set permissions
RUN chmod +x /run.sh

CMD ["/run.sh"]
