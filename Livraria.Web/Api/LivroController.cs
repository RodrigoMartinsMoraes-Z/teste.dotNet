﻿using AutoMapper;

using Livraria.Domain.Contexto;
using Livraria.Domain.Livros;
using Livraria.Domain.ManyToMany;
using Livraria.Domain.Pessoas;
using Livraria.Web.Models.Livros;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Livraria.Web.Api
{
    [Route("api/livro")]
    public class LivroController : BaseApiController
    {
        private readonly IContextoDeDados _contexto;
        private readonly IMapper _mapper;

        public LivroController(IContextoDeDados contexto, IMapper mapper)
        {
            _contexto = contexto;
            _mapper = mapper;
        }
           

        [HttpGet, Route("{take}/{skip}"), AllowAnonymous]
        public ActionResult BuscarLivros(int take, int skip, string busca = null, string tema = null)
        {
            IQueryable<Livro> livros = null;


            if (busca != null)
                livros = _contexto.Livros.Where(l =>
                                                l.Autores.Any(a => a.Autor.Nome.Contains(busca)) ||
                                                l.Titulo.Contains(busca)
                                                ).Take(take).Skip(skip);
            else
                livros = _contexto.Livros.Take(take).Skip(skip);

            livros = livros.OrderBy(l => l.Titulo);

            if (tema != null)
            {
                livros = livros.Where(l => l.Temas.Any(t => t.Tema.Valor == tema));
            }

            List<LivroModel> livrosModel = new List<LivroModel>();

            foreach (Livro livro in livros)
            {
                var autores = _contexto.AutoresLivros.Where(al => al.IdLivro == livro.Id).ToList();
                foreach (var autorLivro in autores)
                {
                    autorLivro.Autor = _contexto.Pessoas.Find(autorLivro.IdAutor);
                    livro.Autores.Add(autorLivro);
                }

                List<LivroTema> temas = _contexto.LivrosTemas.Where(al => al.IdLivro == livro.Id).ToList();
                foreach (var livroTema in temas)
                {
                    livroTema.Tema = _contexto.Temas.Find(livroTema.IdTema);
                    livro.Temas.Add(livroTema);
                }
                LivroModel model = _mapper.Map<LivroModel>(livro);
                livrosModel.Add(model);
            }

            return Ok(livrosModel);
        }

        [HttpPut]
        public async Task<ActionResult> AtualizarLivro([FromBody] LivroModel livroModel)
        {
            Livro livro = _mapper.Map<Livro>(livroModel);

            if (_contexto.Livros.Any(l => l.Titulo == livroModel.Titulo && l.Id != livroModel.Id))
                return BadRequest("Livro ja cadastrado.");

            if (livro.Id > 0)
                _contexto.Livros.Update(livro);
            else
                _contexto.Livros.Add(livro);

            _contexto.SaveChanges();

            await AtualizarTemas(livroModel, livro);

            await AtualizarAutores(livroModel, livro);

            _contexto.SaveChanges();

            return Ok(_mapper.Map<LivroModel>(livro));
        }

        private Task AtualizarAutores(LivroModel livroModel, Livro livro)
        {
            foreach (Models.Pessoas.PessoaModel autor in livroModel.Autores)
            {
                var autorExistente = _contexto.Pessoas.FirstOrDefault(a => a.Nome == autor.Nome);
                if (autorExistente != null)
                {
                    if (livro.Autores != null && livro.Autores.Count > 0 && livro.Autores.Any(t => t.Autor != autorExistente))
                    {
                        var livroAutor = _contexto.AutoresLivros.FirstOrDefault(lt => lt.IdAutor == autorExistente.Id);
                        if (livroAutor == null)
                            livroAutor = new AutorLivro { IdAutor = autorExistente.Id, Autor = autorExistente, Livro = livro, IdLivro = livro.Id };

                        livro.Autores.Add(livroAutor);
                        _contexto.SaveChanges();
                    }
                    else
                    {
                        var livroAutor = _contexto.AutoresLivros.FirstOrDefault(lt => lt.IdAutor == autorExistente.Id);
                        livro.Autores.Add(livroAutor);
                        _contexto.SaveChanges();
                    }
                }
                else
                {
                    Pessoa pessoaAutor = new Pessoa { Nome = autor.Nome };
                    _contexto.Add(pessoaAutor);
                    _contexto.SaveChanges();

                    AutorLivro autorLivro = new AutorLivro { Autor = pessoaAutor, IdAutor = pessoaAutor.Id, IdLivro = livro.Id, Livro = livro };
                    _contexto.Add(autorLivro);
                    _contexto.SaveChanges();
                    livro.Autores.Add(autorLivro);
                    _contexto.SaveChanges();
                }
            }

            return Task.CompletedTask;
        }

        private Task AtualizarTemas(LivroModel livroModel, Livro livro)
        {
            foreach (string tema in livroModel.Temas)
            {
                var temaExistente = _contexto.Temas.FirstOrDefault(t => t.Valor == tema);
                if (temaExistente != null)
                {
                    if (livro.Temas != null && livro.Temas.Count > 0 && livro.Temas.Any(t => t.Tema != temaExistente))
                    {
                        LivroTema livroTema = _contexto.LivrosTemas.FirstOrDefault(lt => lt.IdTema == temaExistente.Id);
                        if (livroTema == null)
                            livroTema = new LivroTema { IdTema = temaExistente.Id, Tema = temaExistente, Livro = livro, IdLivro = livro.Id };

                        livro.Temas.Add(livroTema);
                        _contexto.SaveChanges();
                    }
                    else
                    {
                        LivroTema livroTema = _contexto.LivrosTemas.FirstOrDefault(lt => lt.IdTema == temaExistente.Id);
                        livro.Temas.Add(livroTema);
                        _contexto.SaveChanges();
                    }
                }
                else
                {
                    Tema novoTema = new Tema { Valor = tema };
                    _contexto.Add(novoTema);
                    _contexto.SaveChanges();

                    LivroTema livroTema = new LivroTema { Tema = novoTema, IdTema = novoTema.Id, IdLivro = livro.Id, Livro = livro };
                    _contexto.Add(livroTema);
                    _contexto.SaveChanges();
                    livro.Temas.Add(livroTema);
                    _contexto.SaveChanges();
                }
            }

            return Task.CompletedTask;
        }
    }
}
