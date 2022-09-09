using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mintc
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Count() > 3)
            {
                string nom_reseau = args[0];
                string nom_matrice = args[1];
                string nom_sortie = args[2];
                string nom_parametres = args[3];
                affectation_tc(nom_reseau, nom_matrice, nom_sortie, nom_parametres);
            }


        }

        public static void affectation_tc(string nom_reseau, string nom_matrice, string nom_sortie, string nom_parametres)
        {


            int id_bucket = 0;
            List<List<int>> touches = new List<List<int>>();
            Network reseau = new Network();
            Matrix matrice = new Matrix();
            SortedDictionary<ReachLink, int> reached = new SortedDictionary<ReachLink, int>(new ReachLinkComparer());
            reseau = ReadNetworkFromCsv(nom_reseau);
            matrice = ReadMatrixFromCsv(nom_matrice);
            string nom_fichier = nom_sortie;

            System.IO.StreamReader parametres = new System.IO.StreamReader(nom_parametres);

            reseau.cwait = float.Parse(parametres.ReadLine());
            reseau.cmap = float.Parse(parametres.ReadLine());
            reseau.tboa = float.Parse(parametres.ReadLine());
            reseau.cboa = float.Parse(parametres.ReadLine());
            reseau.output_strategies = bool.Parse(parametres.ReadLine());
            reseau.alg_parameter = float.Parse(parametres.ReadLine());
            //float parametre = float.Parse(parametres.ReadLine());
            parametres.Close();


            System.IO.StreamWriter fich_stg = new System.IO.StreamWriter(nom_fichier + "_stg.txt", false);
            System.IO.StreamWriter fich_od = new System.IO.StreamWriter(nom_fichier + "_od.txt", false);
            fich_od.WriteLine("o;d;temps;cout;ncorr");
            System.IO.StreamWriter fich_times = new System.IO.StreamWriter(nom_fichier + "_times.txt", false);
            fich_times.WriteLine("d;id;i;j;line;ij;hdwy;time;cout;temps");



            //ReachInit(reseau);
            //MessageBox.Show("Lecture terminée");

            int avance = 0;
            int ctop = Console.CursorTop;
            int cleft = Console.CursorLeft;
            foreach (List<Trip> list_trip in matrice.trips.Values)
            {
                avance++;
                NetworkInit(reseau);
                Console.SetCursorPosition(cleft, ctop);
                Console.Write("Destinations: " + 100 * avance / matrice.trips.Count + "/" + matrice.trips.Count);
                Link pivot = new Link();
                pivot.line = "0";
                pivot.hdwy = 0;
                pivot.m = 0;
                pivot.M = 0;
                pivot.id = -1;
                Strategy s = new Strategy();
                s.line = "0";
                s.n = 0;
                s.p = 1;
                s.pivot = -1;
                s.t = 0;
                s.T = 0;
                s.c = 0;
                s.C = 0;
                pivot.strategies.Add(pivot.line, s);
                UpdateProp(pivot, reseau);
                UpdateTime(pivot, reseau);
                id_bucket = 0;
                touches.Clear();
                if (reseau.node_num.ContainsKey(list_trip[0].d) == true)
                {
                    Node noeud = reseau.nodes[reseau.node_num[list_trip[0].d]];
                    foreach (int link_id in noeud.incoming)
                    {
                        TryReach(reseau.links[link_id], pivot, touches, id_bucket, reseau);
                    }
                }
                else
                {
                    //destination inconnue
                }
                int pos = 0;
                while (id_bucket < touches.Count)
                {


                    while (touches[id_bucket].Count == 0)
                    {
                        id_bucket++;
                        if (id_bucket == touches.Count)
                        {
                            goto fin;
                        }
                    }
                    int id_pivot = -1;
                    //float cout_max = 1e38f;
                    /*foreach (int k in touches[id_bucket])
                     {
                         if (reseau.links[k].avg_cout< cout_max)
                         {
                             cout_max = reseau.links[k].avg_cout;
                             id_pivot = k;

                         }
                     }*/
                    id_pivot = touches[id_bucket][0];

                    //                   PrintStrategies(reseau, fich_stg, list_trip[0]);
                    //                              MessageBox.Show(id_pivot + " " + cout_max);

                    reseau.links[id_pivot].reached = 2;
                    pos++;
                    reseau.links[id_pivot].pos = pos;
                    touches[id_bucket].Remove(id_pivot);

                    foreach (int link_id in reseau.nodes[reseau.node_num[reseau.links[id_pivot].i]].incoming)
                    {
                        // fich_stg.WriteLine("\n"+reseau.links[link_id].i + " " + reseau.links[link_id].j + " " + reseau.links[link_id].line + " " + reseau.links[id_pivot].i + " " + reseau.links[id_pivot].j + " " + reseau.links[id_pivot].line + " " + reseau.links[link_id].reached+" "+reseau.links[id_pivot].reached);
                        //                     PrintStrategies(reseau,fich_stg,list_trip[0]);
                        if ((reseau.links[link_id].line != reseau.links[id_pivot].line) || (reseau.links[link_id].hdwy ==0 &&  reseau.links[id_pivot].hdwy == 0))
//                            if ((reseau.links[id_pivot].j != reseau.links[link_id].i))// || (reseau.links[link_id].hdwy != 0 || reseau.links[id_pivot].hdwy != 0))
                        //if (!((reseau.links[id_pivot].i == reseau.links[link_id].j) & (reseau.links[link_id].hdwy == reseau.links[id_pivot].hdwy) & (reseau.links[link_id].hdwy*reseau.links[id_pivot].hdwy>0)))
                            {
                                if (reseau.links[link_id].reached == 0)
                            {
                                TryReach(reseau.links[link_id], reseau.links[id_pivot], touches, id_bucket, reseau);
                            }
                            else if (reseau.links[link_id].reached >= 1 && (reseau.links[id_pivot].path_elements.Contains(link_id) == false))
                            {
                                TryOptimize(reseau.links[link_id], reseau.links[id_pivot], touches, id_bucket, reseau);
                            }
                        }
                    }

                }
            fin:;
                if (reseau.output_strategies == true)
                {
                    PrintStrategies(reseau, fich_stg, list_trip[0]);
                }
                PrintTimes(reseau, fich_times, list_trip[0]);

                //                PrintStrategies(reseau,fich_stg,list_trip[0]);
                foreach (Trip od in list_trip)
                {
                    Assign(reseau, od, fich_od);
                }
            }
            PrintAssignment(reseau, nom_fichier + "_aff.txt");
            fich_od.Close();
            fich_stg.Close();
            fich_times.Close();


        }

        public static Network ReadNetworkFromCsv(String filename)
        {
            string chaine;
            String[] delim = { ";" };
            Network reseau = new Network();

            System.IO.StreamReader nom_reseau = new System.IO.StreamReader(filename);
            do
            {
                chaine = nom_reseau.ReadLine();
                Link lien;
                lien = ReadLinkCard(chaine, delim);
                reseau.AddLink(lien);


            } while (nom_reseau.EndOfStream == false);
            nom_reseau.Close();
            return reseau;
        }
        public static void NetworkInit(Network network)
        {
            foreach (Link link in network.links)
            {
                link.strategies.Clear();
                link.path_elements.Clear();
                link.reached = 0;
                link.m = 0;
                link.M = 0;
                link.n = 0;
                link.allowalij = 1;
                link.allowboai = 1;
                link.avg_time = 0;
                link.avg_cout = 0;
                link.M0 = 0;


            }
        }
        public static Link ReadLinkCard(String chaine, String[] delim)
        {

            String[] ch;
            Link lien = new Link();
            ch = chaine.Split(delim, StringSplitOptions.None);
            if (ch.Length >= 8)
            {
                lien.i = ch[0].Trim();
                lien.j = ch[1].Trim();
                lien.line = ch[2].Trim();
                lien.time = float.Parse(ch[3]);
                lien.hdwy = float.Parse(ch[4]);
                lien.vcap = float.Parse(ch[5]);
                lien.allowboai = int.Parse(ch[6]);
                lien.allowalij = int.Parse(ch[7]);
            }

            return lien;

        }
        public static Matrix ReadMatrixFromCsv(String filename)
        {
            string chaine;
            String[] delim = { ";" };
            Matrix matrice = new Matrix();
            System.IO.StreamReader nom_matrice = new System.IO.StreamReader(filename);
            do
            {
                chaine = nom_matrice.ReadLine();
                Trip trip;
                trip = ReadTripCard(chaine, delim);
                matrice.AddTrip(trip);


            } while (nom_matrice.EndOfStream == false);
            nom_matrice.Close();
            return matrice;
        }
        public static Trip ReadTripCard(String chaine, String[] delim)
        {

            String[] ch;
            Trip trip = new Trip();
            ch = chaine.Split(delim, StringSplitOptions.None);
            if (ch.Length >= 3)
            {
                trip.o = ch[0];
                trip.d = ch[1];
                trip.demand = float.Parse(ch[2]);
            }

            return trip;

        }
        public static void Assign(Network network, Trip trip, System.IO.StreamWriter fich_od)
        {


            if (network.node_num.ContainsKey(trip.o))
            {
                float cout = 1e38f, temps = 0;
                int id_cout = -1;
                Dictionary<int, float> liens = new Dictionary<int, float>();
                float transfer_ratio = 0;

                foreach (int link_id in network.nodes[network.node_num[trip.o]].outgoing)
                {
                    if (network.links[link_id].avg_cout < cout && network.links[link_id].reached != 0)
                    {
                        cout = network.links[link_id].avg_cout;
                        temps = network.links[link_id].avg_time;
                        id_cout = link_id;
                    }


                }
                if (id_cout != -1)
                {
                    if (liens.ContainsKey(id_cout) == false)
                    {
                        liens.Add(id_cout, trip.demand);
                        if (network.links[id_cout].hdwy > 0)
                        {
                            network.links[id_cout].boai += trip.demand;
                            transfer_ratio += trip.demand;
                        }
                    }
                }
                while (liens.Count > 0)
                {
                    float volau = -1;
                    // int lien = liens.Keys.ElementAt(0); id_cout = lien;
                    foreach (int lien in liens.Keys)

                    {
                        if (network.links[lien].pos > volau)
                        {
                            volau = network.links[lien].pos;
                            id_cout = lien;
                        }
                    }
                    network.links[id_cout].volume += liens[id_cout];
                    float demand = liens[id_cout];

                    foreach (Strategy s in network.links[id_cout].strategies.Values)
                    {

                        //   fich_det.WriteLine(link_id+" "+reseau.links[link_id].volume+" "+s.pivot);
                        if (s.pivot != -1 && demand > 1e-8)
                        {
                            if (liens.ContainsKey(s.pivot) == false)
                            {

                                liens.Add(s.pivot, (s.p * demand));
                            }
                            else
                            {
                                liens[s.pivot] += s.p * demand;
                            }


                            if (network.links[s.pivot].hdwy > 0 && network.links[s.pivot].line != network.links[id_cout].line)
                            {
                                network.links[s.pivot].boai += s.p * demand;
                                transfer_ratio += s.p * demand;

                            }
                            if (network.links[id_cout].hdwy > 0 && network.links[s.pivot].line != network.links[id_cout].line)
                            {
                                network.links[id_cout].alij += s.p * demand;

                            }

                        }
                        else if (network.links[id_cout].hdwy > 0)
                        {
                            network.links[id_cout].alij += s.p * demand;
                        }
                    }

                    //MessageBox.Show(network.links[id_cout].id+" "+liens.Count+" "+demand);


                    liens.Remove(id_cout);

                }


                //                AssignLink(network, id_cout,  trip.demand,trip, fich_od,liens);
                fich_od.WriteLine(trip.o + ";" + trip.d + ";" + temps + ";" + cout + ";" + (transfer_ratio / trip.demand));

            }
        }
        public static void TryReach(Link incoming, Link pivot, List<List<int>> touches, int id_bucket, Network reseau)
        {
            if (((incoming.allowalij == 1 && pivot.allowboai == 1) || (pivot.line == incoming.line)))
            {
                if (incoming.line != pivot.line && pivot.hdwy > 0)
                {
                    Strategy strategy = new Strategy();
                    strategy.line = pivot.line;
                    strategy.hdwy = pivot.hdwy;
                    strategy.t = pivot.avg_time + incoming.time;
                    strategy.T = strategy.t + strategy.hdwy;
                    if (incoming.hdwy == 0)
                    {
                        strategy.c = pivot.avg_cout + incoming.time * reseau.cmap + reseau.tboa * reseau.cboa;
                    }
                    else
                    {
                        strategy.c = pivot.avg_cout + incoming.time + reseau.tboa * reseau.cboa;
                    }
                    strategy.C = strategy.c + strategy.hdwy * reseau.cwait;
                    strategy.n = ServiceNumberCalc(strategy.hdwy);
                    strategy.p = 1;
                    strategy.pivot = pivot.id;
                    incoming.strategies.Add(pivot.line, strategy);
                    incoming.n = strategy.n;
                    if (incoming.hdwy == 0)
                    {
                        incoming.m = pivot.avg_cout + incoming.time * reseau.cmap + reseau.tboa * reseau.cboa;
                    }
                    else
                    {
                        incoming.m = pivot.avg_cout + incoming.time + reseau.tboa * reseau.cboa;
                    }
                    incoming.M = incoming.m + pivot.hdwy * reseau.cwait;
                    incoming.M0 = incoming.M;
                    //UpdateM(incoming,reseau);
                    //UpdateProp(incoming,reseau);
                    //UpdateTime(incoming,reseau);


                }
                else
                {
                    incoming.n = 0;
                    incoming.avg_time = 0;
                    incoming.avg_cout = 0;
                    foreach (Strategy s in pivot.strategies.Values)
                    {
                        Strategy strategy = new Strategy();
                        strategy.hdwy = s.hdwy;
                        if (s.pivot == -1)
                        {
                            strategy.line = incoming.line;
                        }
                        else
                        {
                            strategy.line = s.line;
                        }

                        strategy.n = s.n;
                        strategy.p = s.p;
                        strategy.t = s.t + incoming.time;
                        strategy.T = s.T + incoming.time;
                        if (incoming.hdwy == 0)
                        {
                            strategy.c = s.c + incoming.time * reseau.cmap;
                            strategy.C = s.C + incoming.time * reseau.cmap;


                        }
                        else
                        {
                            strategy.c = s.c + incoming.time;
                            strategy.C = s.C + incoming.time; ;

                        }
                        strategy.pivot = pivot.id;
                        incoming.strategies.Add(strategy.line, strategy);
                        incoming.n += ServiceNumberCalc(s.hdwy);

                    }
                    if (incoming.hdwy == 0)
                    {
                        incoming.m = pivot.m + incoming.time * reseau.cmap;
                        incoming.M = pivot.M + incoming.time * reseau.cmap;
                        incoming.M0 = pivot.M0 + incoming.time * reseau.cmap;

                    }
                    else
                    {
                        incoming.m = pivot.m + incoming.time;
                        incoming.M = pivot.M + incoming.time;
                        incoming.M0 = pivot.M0 + incoming.time;
                    }

                }

                UpdateM(incoming, reseau);
                UpdateProp(incoming, reseau);
                UpdateTime(incoming, reseau);
                AddReached(incoming, touches, id_bucket, reseau.alg_parameter);
                //incoming.pos = pivot.pos + 1;
            }

        }

        public static void AddReached(Link link, List<List<int>> touches, int id_bucket, float parametre)
        {
            float pu = 2;
            int bucket, nb = 10000;
            float param = parametre;
            bucket = (int)Math.Min(Math.Pow(link.avg_cout / param, pu), (float)nb);
            link.reached = 1;
            while (bucket >= touches.Count)
            {
                touches.Add(new List<int>());
            }

            touches[bucket].Add(link.id);
            if (bucket < id_bucket)
            {
                id_bucket = bucket;
            }

        }
        public static void OptimizeReduce(Link incoming, Link pivot, List<List<int>> touches, int id_bucket, Network reseau)
        {
            if ((incoming.allowalij == 1 && pivot.allowboai == 1))
            {
                Link old_link = new Link();
                old_link = incoming.Clone();
                float pu = 2;
                int old_bucket, bucket, n = 10000;
                float param = reseau.alg_parameter;
                float test, bucket_value = 0;
                int pos_value = 0;

                if (incoming.hdwy == 0)
                {
                    test = pivot.avg_cout + incoming.time * reseau.cmap + reseau.tboa * reseau.cboa;

                }
                else
                {
                    test = pivot.avg_cout + incoming.time + reseau.tboa * reseau.cboa;
                }
                if (test < incoming.M0)
                {
                    // on peut optimiser

                    old_bucket = (int)Math.Min(Math.Pow(incoming.avg_cout / param, pu), (float)n);
                    bucket_value = incoming.avg_cout;
                    //touches.Remove(incoming.id);
                    pos_value = incoming.pos;
                    // on génère la stratégie moyenne
                    Strategy s = new Strategy();
                    s.line = pivot.line;
                    s.hdwy = pivot.hdwy;
                    s.t = pivot.avg_time + incoming.time;
                    s.T = s.t + s.hdwy;
                    if (incoming.hdwy == 0)
                    {
                        s.c = pivot.avg_cout + incoming.time * reseau.cmap + reseau.tboa * reseau.cboa;
                        s.C = s.c + s.hdwy * reseau.cwait;
                    }
                    else
                    {
                        s.c = pivot.avg_cout + incoming.time + reseau.tboa * reseau.cboa;
                        s.C = s.c + s.hdwy * reseau.cwait;
                    }


                    s.n = ServiceNumberCalc(s.hdwy);
                    s.p = 1;
                    s.pivot = pivot.id;
                    if (incoming.strategies.ContainsKey(s.line) == true)
                    {
                        float testt;
                        if (incoming.hdwy == 0)
                        {
                            testt = s.c + incoming.time * reseau.cmap + reseau.tboa * reseau.cboa;
                        }
                        else
                        {
                            testt = s.c + incoming.time + reseau.tboa * reseau.cboa;
                        }
                        if (incoming.strategies[s.line].c > testt)
                        {
                            incoming.strategies[s.line] = s;
                            /*   float test2;
                               if (incoming.hdwy == 0)
                               {
                                   test2 = pivot.M + incoming.time*reseau.cmap + pivot.hdwy*reseau.cwait+reseau.tboa*reseau.cboa;
                               }
                               else
                               {
                                   test2 = pivot.M + incoming.time + pivot.hdwy * reseau.cwait + reseau.tboa * reseau.cboa;
                               }
                               if (test2 < incoming.M)
                               {
                                   if (incoming.hdwy == 0)
                                   {
                                       incoming.M = pivot.M + incoming.time * reseau.cmap + pivot.hdwy * reseau.cwait + reseau.tboa * reseau.cboa;
                                       incoming.M0 = Math.Min(incoming.M, incoming.M0);
                                   }
                                   else
                                   {
                                       incoming.M = pivot.M + incoming.time + pivot.hdwy * reseau.cwait + reseau.tboa * reseau.cboa;
                                       incoming.M0 = Math.Min(incoming.M, incoming.M0);

                                   }
                               }
                             */
                            incoming.pos = Math.Max(incoming.pos, pivot.pos + 1);
                        }

                    }
                    else
                    {
                        incoming.strategies.Add(s.line, s);
                        /*float test2;
                        if (incoming.hdwy == 0)
                        {
                            test2 = pivot.M + incoming.time * reseau.cmap + pivot.hdwy * reseau.cwait + reseau.tboa * reseau.cboa;
                        }
                        else
                        {
                            test2 = pivot.M + incoming.time + pivot.hdwy * reseau.cwait + reseau.tboa * reseau.cboa;
                        }
                        
                        if (test2 < incoming.M)
                        {
                            if (incoming.hdwy == 0)
                            {
                                incoming.M = pivot.M + incoming.time * reseau.cmap + pivot.hdwy * reseau.cwait + reseau.tboa * reseau.cboa;
                                incoming.M0 = Math.Min(incoming.M, incoming.M0);
                            }
                            else
                            {
                                incoming.M = pivot.M + incoming.time + pivot.hdwy * reseau.cwait + reseau.tboa * reseau.cboa;
                                incoming.M0 = Math.Min(incoming.M, incoming.M0);

                            }
                        }
                        */
                        incoming.pos = Math.Max(incoming.pos, pivot.pos + 1);
                    }

                    UpdateM(incoming, reseau);
                    UpdateProp(incoming, reseau);
                    UpdateTime(incoming, reseau);

                    if (bucket_value > incoming.avg_cout)
                    {
                        bucket = (int)Math.Min(Math.Pow(incoming.avg_cout / param, pu), (float)n);
                        //if (touches[bucket].Contains(incoming.id) == false)
                        {
                            touches[old_bucket].Remove(incoming.id);
                            touches[bucket].Add(incoming.id);
                            if (bucket < id_bucket)
                            {
                                id_bucket = bucket;
                            }

                        }
                    }
                    else
                    {
                        incoming = old_link.Clone();
                    }

                }
            }

        }
        public static void OptimizeAdd(Link incoming, Link pivot, List<List<int>> touches, int id_bucket, Network reseau)
        {
            //MessageBox.Show("Add "+pivot.i + " " + pivot.j + " " + pivot.line + " " + (pivot.m + incoming.time) + " " + incoming.M);

            if ((incoming.allowalij == 1 && pivot.allowboai == 1) || (incoming.line == pivot.line))
            {

                Link old_link = new Link();
                old_link = incoming.Clone();
                float pu = 2;
                int old_bucket, bucket, n = 10000, pos_value;
                float param = reseau.alg_parameter;
                float testa, bucket_value = 0;//,test0;
                if (incoming.hdwy == 0)
                {
                    testa = pivot.m + incoming.time * reseau.cmap;

                    //test0 = pivot.M0 + incoming.time * reseau.cmap;
                }
                else
                {
                    testa = pivot.m + incoming.time;
                    //test0 = pivot.M0 + incoming.time ;
                }

                if (testa < incoming.M0 /*|| ((incoming.M0 - test0) > 0)*/ )
                {
                    //on peut optimiser
                    // lignes TC
                    old_bucket = (int)Math.Min(Math.Pow(incoming.avg_cout / param, pu), (float)n);
                    bucket_value = incoming.avg_cout;
                    //touches[bucket].Remove(incoming.id);
                    pos_value = incoming.pos;
                    foreach (Strategy s in pivot.strategies.Values)
                    {
                        Strategy strategy = new Strategy();
                        strategy.hdwy = s.hdwy;
                        strategy.line = s.line;
                        strategy.n = s.n;
                        strategy.p = s.p;
                        strategy.pivot = pivot.id;
                        strategy.t = s.t + incoming.time;
                        strategy.T = s.T + incoming.time;
                        if (incoming.hdwy == 0)
                        {
                            strategy.c = s.c + incoming.time * reseau.cmap;
                            strategy.C = s.C + incoming.time * reseau.cmap;
                        }
                        else
                        {
                            strategy.c = s.c + incoming.time;
                            strategy.C = s.C + incoming.time;
                        }

                        if (incoming.strategies.ContainsKey(s.line) == true)
                        {
                            float testt;
                            if (incoming.hdwy == 0)
                            {
                                testt = s.c + incoming.time * reseau.cmap;
                            }
                            else
                            {
                                testt = s.c + incoming.time;
                            }

                            if (incoming.strategies[s.line].c > testt)
                            {
                                incoming.strategies.Remove(s.line);
                                strategy.pivot = pivot.id;
                                incoming.strategies.Add(strategy.line, strategy);
                                /* float test;
                                 if (incoming.hdwy == 0)
                                 {
                                     test = pivot.M + incoming.time * reseau.cmap;
                                 }
                                 else
                                 {
                                     test = pivot.M + incoming.time; 
                                 }
                                 if (test < incoming.M)
                                 {
                                     if (incoming.hdwy == 0)
                                     {
                                         incoming.M = pivot.M + incoming.time*reseau.cmap;
                                         incoming.M0 = pivot.M0 + incoming.time*reseau.cmap;
                                     }
                                     else
                                     {
                                         incoming.M = pivot.M + incoming.time;
                                         incoming.M0 = pivot.M0 + incoming.time;
                                     }
                                 }
                     */
                                incoming.pos = Math.Max(incoming.pos, pivot.pos + 1);
                            }

                        }
                        else if (strategy.c < incoming.M0)
                        {
                            strategy.pivot = pivot.id;

                            incoming.strategies.Add(strategy.line, strategy);

                            /*float test;
                            if (incoming.hdwy == 0)
                            {
                                test = pivot.M + incoming.time * reseau.cmap;
                            }
                            else
                            {
                                test = pivot.M + incoming.time;
                            }
                            if (test < incoming.M)
                            {
                                if (incoming.hdwy == 0)
                                {
                                    incoming.M = pivot.M + incoming.time*reseau.cmap;
                                    incoming.M0 = pivot.M0 + incoming.time*reseau.cmap;
                                }
                                else
                                {
                                    incoming.M = pivot.M + incoming.time ;
                                    incoming.M0 = pivot.M0 + incoming.time ;

                                }
                            }
                      */
                            incoming.pos = Math.Max(incoming.pos, pivot.pos + 1);
                        }
                    }
                    UpdateM(incoming, reseau);
                    UpdateProp(incoming, reseau);
                    UpdateTime(incoming, reseau);
                    if (bucket_value > incoming.avg_cout)
                    {
                        bucket = (int)Math.Min(Math.Pow(incoming.avg_cout / param, pu), (float)n);
                        //if (touches.Contains(incoming.id) == false)
                        {
                            touches[bucket].Add(incoming.id);
                            touches[old_bucket].Remove(incoming.id);
                            if (bucket < id_bucket)
                            {
                                id_bucket = bucket;
                            }
                        }
                        //  touches[bucket].Add(incoming.id);

                    }
                    else
                    {
                        incoming = old_link.Clone();
                    }

                }

            }
        }
        public static void UpdateM(Link incoming, Network reseau)

        {



            int test = 1;
        debut:

            float minutes = 0, nb = 0, mprim = 1e38f, mprim0 = 1e38f;


            foreach (Strategy s in incoming.strategies.Values)
            {
                if ((s.c + s.hdwy * reseau.cwait) < mprim)
                {
                    if (s.hdwy > 0)
                    {
                        mprim = s.c + s.hdwy * reseau.cwait;
                        if (mprim < mprim0)
                        {
                            mprim0 = mprim;
                        }
                    }
                    else
                    {
                        if (mprim0 > s.c + s.hdwy * reseau.cwait)
                        {
                            mprim0 = s.c + s.hdwy * reseau.cwait;
                        }



                    }
                }
            }
            if (mprim == 1e38f)
            {
                mprim = mprim0;
            }
            incoming.M = mprim;
            incoming.M0 = mprim0;

            foreach (Strategy s in incoming.strategies.Values)
            {

                if (s.hdwy > 0)
                {
                    minutes += (mprim - s.c) * s.n;
                    nb += s.n;
                }
            }
            float delta;
            if (nb > 0)
            {
                delta = (minutes - 60 * reseau.cwait) / nb;
            }
            else
            {
                delta = 0;
            }

            incoming.M = mprim - delta;
            incoming.M0 = Math.Min(incoming.M0, incoming.M);
            List<String> strategie_M0 = new List<string>();
            foreach (Strategy s in incoming.strategies.Values)
            {
                if (s.c > incoming.M0)
                {
                    strategie_M0.Add(s.line);

                }


                if (s.c < incoming.m)
                {
                    incoming.m = s.c;
                }
                if (s.hdwy > 0)
                {
                    s.C = incoming.M;
                }
            }
            if (strategie_M0.Count > 0)
            {
                test = 1;
            }
            else
            {
                test = 0;
            }
            foreach (String strategie in strategie_M0)
            {
                incoming.strategies.Remove(strategie);
            }
            if (incoming.M0 > incoming.M)
            {
                incoming.M0 = incoming.M;
            }
            incoming.n = nb;

            if (test == 1)
            {
                goto debut;
            }
            incoming.n = nb;

        }

        public static void TryOptimize(Link incoming, Link pivot, List<List<int>> touches, int id_bucket, Network reseau)
        {

            if (incoming.line != pivot.line && pivot.hdwy > 0)
            {
                //      MessageBox.Show("Reduce " + pivot.i + " " + pivot.j + " " + pivot.line + "\n" +incoming.i+" "+incoming.j+" "+incoming.line+" "+(pivot.m + incoming.time) + " " + incoming.M);

                OptimizeReduce(incoming, pivot, touches, id_bucket, reseau);
            }
            else
            {
                //    MessageBox.Show("Add " + pivot.i + " " + pivot.j + " " + pivot.line + "\n" + incoming.i + " " + incoming.j + " " + incoming.line + " " + (pivot.m + incoming.time) + " " + incoming.M);

                OptimizeAdd(incoming, pivot, touches, id_bucket, reseau);
            }


        }
        public static void ReachInit(Network reseau)
        {
            foreach (Link link in reseau.links)
            {
                link.reached = 0;

            }
        }
        public static void PrintStrategies(Network reseau, System.IO.StreamWriter fich_res, Trip trip)
        {
            fich_res.WriteLine("\n" + trip.d);


            foreach (Link link in reseau.links)
            {
                fich_res.WriteLine(link.id + ";" + link.i + ";" + link.j + ";" + link.line + ";" + link.hdwy + ";" + link.time + ";" + link.m + ";" + link.M + ";" + link.M0 + ";" + link.n + ";" + link.avg_cout + ";" + link.avg_time + ";" + link.pos + ";" + link.reached);

                foreach (Strategy s in link.strategies.Values)
                {
                    fich_res.WriteLine(" " + s.line + ";" + s.hdwy + ";" + s.n + ";" + s.p + ";" + s.c + ";" + s.C + ";" + s.pivot);
                }
            }
            fich_res.WriteLine();

        }

        public static void PrintTimes(Network reseau, System.IO.StreamWriter fich_res, Trip trip)
        {


            foreach (Link link in reseau.links)
            {
                //fich_res.WriteLine("d;id;i;j;line;ij;hdwy;time;cout;temps");
                if (link.avg_cout > 0)
                {
                    fich_res.WriteLine(trip.d + ";" + link.id + ";" + link.i + ";" + link.j + ";" + link.line + ";" + link.i + "-" + link.j + ";" + link.hdwy + ";" + link.time + ";" + link.avg_cout + ";" + link.avg_time);
                }


            }


        }
        public static void PrintAssignment(Network reseau, String nom_fichier)
        {
            System.IO.StreamWriter fich_aff = new System.IO.StreamWriter(nom_fichier, false);
            fich_aff.WriteLine("num;i;j;ij;line;volau;boai;alij");
            foreach (Link link in reseau.links)
            {
                if (link.volume > 0)
                {
                    fich_aff.WriteLine(link.id + ";" + link.i + ";" + link.j + ";" + link.i + "-" + link.j + ";" + link.line + ";" + link.volume + ";" + link.boai + ";" + link.alij);
                }
            }
            fich_aff.Close();

        }

        public static float ServiceNumberCalc(float hdw)
        {
            if (hdw == 0)
            {
                return 0;
            }
            else
            {
                return (60 / hdw);
            }

        }
        public static void UpdateProp(Link link, Network reseau)
        {
            link.path_elements.Clear();
            foreach (Strategy s in link.strategies.Values)
            {
                if (s.hdwy > 0)
                {
                    s.p = (link.M0 - s.c) / (s.hdwy * reseau.cwait);
                }
                else
                {
                    if (link.n > 0)
                    {
                        s.p = (link.M - link.M0) / (ServiceNumberCalc(link.n) * reseau.cwait);
                    }
                    else
                    {
                        s.p = 1;
                    }
                }
                if (s.pivot != -1)
                {
                    Link pivot = reseau.links[s.pivot];
                    link.path_elements.UnionWith(pivot.path_elements);
                    link.path_elements.Add(s.pivot);
                    /*foreach (int element in pivot.path_elements.Keys)
                    {
                            link.path_elements[element]=element;
                    }
                        link.path_elements[s.pivot]=s.pivot;*/
                }

            }

        }
        public static void UpdateTime(Link link, Network reseau)
        {
            link.avg_time = 0;
            link.avg_cout = 0;
            foreach (Strategy s in link.strategies.Values)
            {
                link.avg_cout += 0.5f * (s.c + link.M0) * s.p;
                link.avg_time += (s.t + 0.5f * ((link.M0 - s.c) / reseau.cwait)) * s.p;

            }
        }


    }

    public class Link
    {
        public string i, j, line;
        public float vcap = 0, time = 0, hdwy = 0, n = 0;
        public float m, M, M0, avg_time = 0, volume = 0, avg_cout = 0, alij = 0, boai = 0, nb_trips = 0;
        public int allowboai = 1, allowalij = 1, id, reached = 0, pos = -1;
        public Dictionary<String, Strategy> strategies = new Dictionary<String, Strategy>();
        public HashSet<int> path_elements = new HashSet<int>();
        public Link Clone()
        {
            return (Link)this.MemberwiseClone();
        }
    }
    public class Strategy
    {
        public string line = null;
        public float t = 0, T = 0, p = 1, hdwy = 0, n = 0, c = 0, C = 0;
        public int pivot = -1;

    }
    public class Node
    {
        public string i;
        public float x, y;
        public List<int> incoming = new List<int>();
        public List<int> outgoing = new List<int>();
    }
    public class Network
    {
        public List<Node> nodes = new List<Node>();
        public List<Link> links = new List<Link>();
        public Dictionary<string, int> node_num = new Dictionary<string, int>();
        public Dictionary<Link_num, int> link_num = new Dictionary<Link_num, int>();
        public float cwait = 0, tboa = 0, cboa = 0, cmap = 0, alg_parameter = 10;
        public bool output_strategies = true;
        public void AddNode(Node node)
        {
            node_num[node.i] = nodes.Count();
            nodes.Add(node);

        }

        public void AddLink(Link link)
        {
            Link_num num_link = new Link_num();

            num_link.i = link.i;
            if (node_num.ContainsKey(link.i) == false)
            {
                Node noeud = new Node();
                noeud.i = link.i;
                node_num[link.i] = nodes.Count();
                nodes.Add(noeud);
            }
            num_link.j = link.j;
            if (node_num.ContainsKey(link.j) == false)
            {
                Node noeud = new Node();
                noeud.i = link.j;
                node_num[link.j] = nodes.Count();
                nodes.Add(noeud);
            }

            num_link.line = link.line;
            link_num[num_link] = links.Count();
            link.id = links.Count;
            links.Add(link);

            nodes[node_num[link.i]].outgoing.Add(link.id);
            nodes[node_num[link.j]].incoming.Add(link.id);

        }


    }

    public class Link_num
    {
        public String i, j, line;
        public override bool Equals(object num_link)
        {
            if (i == ((Link_num)num_link).i && j == ((Link_num)num_link).j && line == ((Link_num)num_link).line)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public override int GetHashCode()
        {
            return (i.GetHashCode() ^ j.GetHashCode() ^ line.GetHashCode());
        }
    }

    public class Trip
    {
        public String o, d;
        public float demand;
    }

    public class Matrix
    {
        public Dictionary<String, List<Trip>> trips = new Dictionary<string, List<Trip>>();

        public void AddTrip(Trip trip)
        {
            if (trips.ContainsKey(trip.d) == false)
            {
                trips.Add(trip.d, new List<Trip>());
            }
            trips[trip.d].Add(trip);
        }
    }
    public class ReachLink
    {
        public int numlink;
        public float time;
        public ReachLink(float M, int Id)
        {
            time = M;
            numlink = Id;
        }
    }
    public class ReachLinkComparer : IComparer<ReachLink>
    {

        // compare the distance from the origin of one point and  another point.
        public int Compare(ReachLink x, ReachLink y)
        {
            if (x.time > y.time)
            {
                return 1;
            }
            else
            {
                return -1;
            }

        }
    }


}
