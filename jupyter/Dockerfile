FROM jupyter/base-notebook:latest

RUN pip install \
       numpy pandas statsmodels matplotlib seaborn shapely geopandas[all] \


RUN pip cache purge

CMD ["start-notebook.sh"]