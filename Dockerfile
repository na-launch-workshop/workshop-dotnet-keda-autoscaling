FROM registry.access.redhat.com/ubi9/dotnet-80:latest AS build
WORKDIR /src
COPY src/KedaWorkshop/KedaWorkshop.csproj src/KedaWorkshop/
RUN dotnet restore src/KedaWorkshop/KedaWorkshop.csproj
COPY src/KedaWorkshop/ src/KedaWorkshop/
RUN dotnet publish src/KedaWorkshop/KedaWorkshop.csproj -c Release -o /app/publish

FROM registry.access.redhat.com/ubi9/dotnet-80-runtime:latest
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
ENTRYPOINT ["dotnet", "KedaWorkshop.dll"]
