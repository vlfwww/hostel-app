FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY HotelApi/HotelApi.csproj HotelApi/
RUN dotnet restore HotelApi/HotelApi.csproj
COPY HotelApi/ HotelApi/
RUN dotnet publish HotelApi/HotelApi.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "HotelApi.dll"]
