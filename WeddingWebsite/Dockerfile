FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /App
ENV ASPNETCORE_HTTP_PORTS=8080

# Copy everything
COPY . ./
# Restore as distinct layers
RUN dotnet restore

# ENV PGSQLROOTCERT="db-urchin-ca-certificate.crt"

# install dotnet-ef

# RUN dotnet tool install --create-manifest-if-needed dotnet-ef
# # create /update database
# RUN dotnet ef database update

# Build and publish a release
RUN dotnet publish -o out

# dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL --version 9

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0
ENV ASPNETCORE_HTTP_PORTS=8080
WORKDIR /App
COPY --from=build /App/"Data\app.db" .
# COPY --from=build /App/db-urchin-ca-certificate.crt .
COPY --from=build /App/out .
COPY --from=build /App/PrivatePhotos ./PrivatePhotos
# ENV PGSQLROOTCERT="db-urchin-ca-certificate.crt"
ENTRYPOINT ["dotnet", "WeddingWebsite.dll"]
EXPOSE 8080

