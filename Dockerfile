FROM microsoft/dotnet:1.0.5-sdk 
COPY . /src/app 
WORKDIR /src/app 
RUN ["dotnet", "migrate"]
RUN ["dotnet", "restore"]
RUN ["dotnet", "build"]
EXPOSE 60002/tcp
ENV ASPNETCORE_URLS http://*:60002
ENTRYPOINT ["dotnet", "run"]
