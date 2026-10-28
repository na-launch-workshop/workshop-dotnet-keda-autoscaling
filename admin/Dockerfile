FROM registry.access.redhat.com/ubi8/dotnet-80:8.0 AS build
COPY --chown=1001:0 . .
RUN dotnet publish ScoreBoard -c Release -o /opt/app-root/publish/scoreboard && \
    dotnet publish Surge -c Release -o /opt/app-root/publish/surge

FROM registry.access.redhat.com/ubi8/dotnet-80-runtime:8.0
COPY --from=build /opt/app-root/publish/scoreboard .
# surge-all.sh runs the surge from this same image: dotnet /opt/app-root/surge/Surge.dll
COPY --from=build /opt/app-root/publish/surge /opt/app-root/surge
EXPOSE 8080
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
CMD ["dotnet", "ScoreBoard.dll"]
