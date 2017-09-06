FROM Microsoft/dotnet:latest 
COPY . /app 
WORKDIR /app 
RUN ["dotnet", "restore"]
RUN ["dotnet", "build"]
EXPOSE 60002/tcp
ENV ASPNETCORE_URLS http://*:60002
ENTRYPOINT ["dotnet", "run"]
